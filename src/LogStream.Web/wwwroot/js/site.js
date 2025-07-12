// Dark mode toggle
window.toggleDarkMode = (isDarkMode) => {
    if (isDarkMode) {
        document.body.classList.add('dark-theme');
    } else {
        document.body.classList.remove('dark-theme');
    }
};

// Chart.js helper functions
window.updatePieChart = (canvasId, data) => {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    
    // Destroy existing chart if it exists
    if (canvas.chart) {
        canvas.chart.destroy();
    }

    canvas.chart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: data.map(d => d.label),
            datasets: [{
                data: data.map(d => d.value),
                backgroundColor: data.map(d => d.color),
                borderWidth: 2,
                borderColor: '#ffffff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
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
        }
    });
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

// Performance monitoring
window.performanceMonitor = {
    startTime: null,
    
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
    }
};

// Error handling
window.addEventListener('error', (event) => {
    console.error('Global error:', event.error);
});

window.addEventListener('unhandledrejection', (event) => {
    console.error('Unhandled promise rejection:', event.reason);
});

// Load Chart.js library
(function() {
    const script = document.createElement('script');
    script.src = 'https://cdn.jsdelivr.net/npm/chart.js';
    script.onload = () => {
        console.log('Chart.js loaded successfully');
    };
    document.head.appendChild(script);
})();