// Performance optimizations
window.chartInstances = new Map();
window.intersectionObservers = new Map();
window.resizeObservers = new Map();

// Dark mode toggle with performance improvements
window.toggleDarkMode = (isDarkMode) => {
    document.body.classList.toggle('dark-theme', isDarkMode);
    
    // Save to localStorage
    try {
        localStorage.setItem('darkMode', isDarkMode.toString());
    } catch (error) {
        console.warn('Failed to save dark mode preference:', error);
    }
    
    // Update all charts for theme change
    window.chartInstances.forEach((chart, chartId) => {
        try {
            chart.update('none'); // No animation for better performance
        } catch (error) {
            console.error(`Error updating chart ${chartId} for theme change:`, error);
        }
    });
};

// Optimized Chart.js helper functions
window.initializeChart = (chartId, type, data, options) => {
    try {
        const canvas = document.getElementById(chartId);
        if (!canvas) {
            console.error(`Canvas with id ${chartId} not found`);
            return;
        }

        // Destroy existing chart if it exists
        if (window.chartInstances.has(chartId)) {
            window.chartInstances.get(chartId).destroy();
        }

        const ctx = canvas.getContext('2d');
        const chart = new Chart(ctx, {
            type: type,
            data: data,
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: {
                    duration: 0 // Disable animations for better performance
                },
                ...options
            }
        });

        window.chartInstances.set(chartId, chart);
    } catch (error) {
        console.error(`Error initializing chart ${chartId}:`, error);
    }
};

window.updateChartData = (chartId, data) => {
    try {
        const chart = window.chartInstances.get(chartId);
        if (chart) {
            chart.data = data;
            chart.update('none'); // No animation for better performance
        }
    } catch (error) {
        console.error(`Error updating chart data ${chartId}:`, error);
    }
};

window.updatePieChart = (canvasId, data) => {
    const chart = window.chartInstances.get(canvasId);
    if (chart) {
        chart.data = {
            labels: data.map(d => d.label),
            datasets: [{
                data: data.map(d => d.value),
                backgroundColor: data.map(d => d.color),
                borderWidth: 2,
                borderColor: '#ffffff'
            }]
        };
        chart.update('none');
    } else {
        window.initializeChart(canvasId, 'doughnut', {
            labels: data.map(d => d.label),
            datasets: [{
                data: data.map(d => d.value),
                backgroundColor: data.map(d => d.color),
                borderWidth: 2,
                borderColor: '#ffffff'
            }]
        }, {
            plugins: {
                legend: {
                    position: 'bottom'
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = ((context.parsed / total) * 100).toFixed(1);
                            return `${context.label}: ${context.parsed.toLocaleString()} (${percentage}%)`;
                        }
                    }
                }
            }
        });
    }
};

window.updateLineChart = (canvasId, data, labels) => {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    
    // Destroy existing chart if it exists
    if (canvas.chart) {
        canvas.chart.destroy();
    }

    canvas.chart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: data
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true
                }
            },
            plugins: {
                tooltip: {
                    mode: 'index',
                    intersect: false
                }
            }
        }
    });
};

// Utility functions
window.formatNumber = (number) => {
    return new Intl.NumberFormat().format(number);
};

window.copyToClipboard = async (text) => {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (err) {
        console.error('Failed to copy text: ', err);
        return false;
    }
};

// Auto-refresh functionality
window.autoRefresh = {
    timers: {},
    
    start: (id, callback, intervalMs) => {
        if (window.autoRefresh.timers[id]) {
            clearInterval(window.autoRefresh.timers[id]);
        }
        
        window.autoRefresh.timers[id] = setInterval(callback, intervalMs);
    },
    
    stop: (id) => {
        if (window.autoRefresh.timers[id]) {
            clearInterval(window.autoRefresh.timers[id]);
            delete window.autoRefresh.timers[id];
        }
    },
    
    stopAll: () => {
        Object.values(window.autoRefresh.timers).forEach(clearInterval);
        window.autoRefresh.timers = {};
    }
};

// Intersection Observer for lazy loading
window.setupIntersectionObserver = (element, dotNetRef, rootMargin = '0px') => {
    if (!element) return;

    const observerId = Math.random().toString(36).substring(7);
    
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                dotNetRef.invokeMethodAsync('OnIntersecting');
                observer.unobserve(entry.target);
                window.intersectionObservers.delete(observerId);
            }
        });
    }, {
        rootMargin: rootMargin,
        threshold: 0.1
    });

    observer.observe(element);
    window.intersectionObservers.set(observerId, observer);
    
    return observerId;
};

// Debounced resize handler
window.debounce = (func, wait) => {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
};

// Virtualized list scroll functions
window.scrollVirtualizedListToTop = (selector) => {
    const container = document.querySelector(selector);
    if (container) {
        container.scrollTop = 0;
    }
};

window.scrollVirtualizedListToBottom = (selector) => {
    const container = document.querySelector(selector);
    if (container) {
        container.scrollTop = container.scrollHeight;
    }
};

window.scrollVirtualizedListToItem = (selector, index, itemSize) => {
    const container = document.querySelector(selector);
    if (container) {
        container.scrollTop = index * itemSize;
    }
};

// Enhanced performance monitoring
window.performanceMonitor = {
    startTime: null,
    marks: new Map(),
    
    start: () => {
        window.performanceMonitor.startTime = performance.now();
    },
    
    end: (operation) => {
        if (window.performanceMonitor.startTime) {
            const duration = performance.now() - window.performanceMonitor.startTime;
            console.log(`${operation} took ${duration.toFixed(2)} ms`);
            window.performanceMonitor.startTime = null;
            return duration;
        }
        return 0;
    },
    
    markStart: (name) => {
        if (performance.mark) {
            performance.mark(`${name}-start`);
        }
        window.performanceMonitor.marks.set(name, performance.now());
    },
    
    markEnd: (name) => {
        const startTime = window.performanceMonitor.marks.get(name);
        if (startTime) {
            const duration = performance.now() - startTime;
            console.log(`${name}: ${duration.toFixed(2)}ms`);
            window.performanceMonitor.marks.delete(name);
            
            if (performance.mark && performance.measure) {
                performance.mark(`${name}-end`);
                performance.measure(name, `${name}-start`, `${name}-end`);
            }
            
            return duration;
        }
        return 0;
    }
};

// Error handling
window.addEventListener('error', (event) => {
    console.error('Global error:', event.error);
});

window.addEventListener('unhandledrejection', (event) => {
    console.error('Unhandled promise rejection:', event.reason);
});

// Initialize theme on page load
document.addEventListener('DOMContentLoaded', () => {
    try {
        const savedTheme = localStorage.getItem('darkMode');
        if (savedTheme === 'true') {
            document.body.classList.add('dark-theme');
        }
    } catch (error) {
        console.warn('Failed to load dark mode preference:', error);
    }
});

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    // Clean up chart instances
    window.chartInstances.forEach((chart, chartId) => {
        try {
            chart.destroy();
        } catch (error) {
            console.error(`Error destroying chart ${chartId} on unload:`, error);
        }
    });
    window.chartInstances.clear();
    
    // Clean up observers
    window.intersectionObservers.forEach((observer, id) => {
        try {
            observer.disconnect();
        } catch (error) {
            console.error(`Error disconnecting observer ${id}:`, error);
        }
    });
    window.intersectionObservers.clear();
    
    window.resizeObservers.forEach((observer, id) => {
        try {
            observer.disconnect();
        } catch (error) {
            console.error(`Error disconnecting resize observer ${id}:`, error);
        }
    });
    window.resizeObservers.clear();
    
    // Stop all auto-refresh timers
    window.autoRefresh.stopAll();
});

// Resize handler for responsive charts
window.addEventListener('resize', window.debounce(() => {
    window.chartInstances.forEach((chart, chartId) => {
        try {
            chart.resize();
        } catch (error) {
            console.error(`Error resizing chart ${chartId}:`, error);
        }
    });
}, 250));

// Load Chart.js library
(function() {
    const script = document.createElement('script');
    script.src = 'https://cdn.jsdelivr.net/npm/chart.js';
    script.onload = () => {
        console.log('Chart.js loaded successfully');
    };
    document.head.appendChild(script);
})();