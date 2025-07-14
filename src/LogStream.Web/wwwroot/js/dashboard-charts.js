// LogStream Analytics - Advanced Dashboard Charts
let charts = {};

// Initialize Chart.js with default configuration
Chart.defaults.responsive = true;
Chart.defaults.maintainAspectRatio = false;
Chart.defaults.plugins.legend.display = true;
Chart.defaults.plugins.tooltip.enabled = true;

// Common chart options
const commonOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
        legend: {
            position: 'top',
            labels: {
                usePointStyle: true,
                padding: 20,
                font: {
                    size: 12
                }
            }
        },
        tooltip: {
            backgroundColor: 'rgba(0,0,0,0.8)',
            titleColor: '#fff',
            bodyColor: '#fff',
            borderColor: 'rgba(255,255,255,0.1)',
            borderWidth: 1,
            cornerRadius: 8,
            displayColors: true
        }
    },
    animation: {
        duration: 750,
        easing: 'easeInOutQuart'
    }
};

// Line chart for log volume trends
window.updateLineChart = function(canvasId, data) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    // Destroy existing chart
    if (charts[canvasId]) {
        charts[canvasId].destroy();
    }

    charts[canvasId] = new Chart(ctx, {
        type: 'line',
        data: data,
        options: {
            ...commonOptions,
            scales: {
                x: {
                    grid: {
                        color: 'rgba(0,0,0,0.1)'
                    },
                    ticks: {
                        maxTicksLimit: 12
                    }
                },
                y: {
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(0,0,0,0.1)'
                    },
                    ticks: {
                        callback: function(value) {
                            return value >= 1000 ? (value / 1000).toFixed(1) + 'K' : value;
                        }
                    }
                }
            },
            plugins: {
                ...commonOptions.plugins,
                legend: {
                    ...commonOptions.plugins.legend,
                    display: true
                }
            },
            elements: {
                point: {
                    radius: 3,
                    hoverRadius: 6
                },
                line: {
                    borderWidth: 2,
                    fill: true
                }
            }
        }
    });
};

// Doughnut chart for log level distribution
window.updateDoughnutChart = function(canvasId, data) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    // Destroy existing chart
    if (charts[canvasId]) {
        charts[canvasId].destroy();
    }

    const chartData = {
        labels: data.map(item => item.label),
        datasets: [{
            data: data.map(item => item.value),
            backgroundColor: data.map(item => item.color),
            borderWidth: 0,
            hoverBorderWidth: 2,
            hoverBorderColor: '#fff'
        }]
    };

    charts[canvasId] = new Chart(ctx, {
        type: 'doughnut',
        data: chartData,
        options: {
            ...commonOptions,
            cutout: '60%',
            plugins: {
                ...commonOptions.plugins,
                legend: {
                    position: 'right',
                    labels: {
                        generateLabels: function(chart) {
                            const data = chart.data;
                            if (data.labels.length && data.datasets.length) {
                                return data.labels.map((label, i) => {
                                    const dataset = data.datasets[0];
                                    const value = dataset.data[i];
                                    const total = dataset.data.reduce((a, b) => a + b, 0);
                                    const percentage = ((value / total) * 100).toFixed(1);
                                    
                                    return {
                                        text: `${label}: ${percentage}%`,
                                        fillStyle: dataset.backgroundColor[i],
                                        hidden: false,
                                        index: i
                                    };
                                });
                            }
                            return [];
                        },
                        usePointStyle: true,
                        padding: 15
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const label = context.label || '';
                            const value = context.parsed;
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = ((value / total) * 100).toFixed(1);
                            return `${label}: ${value.toLocaleString()} (${percentage}%)`;
                        }
                    }
                }
            }
        }
    });
};

// Bar chart for application performance
window.updateBarChart = function(canvasId, data) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    // Destroy existing chart
    if (charts[canvasId]) {
        charts[canvasId].destroy();
    }

    charts[canvasId] = new Chart(ctx, {
        type: 'bar',
        data: data,
        options: {
            ...commonOptions,
            indexAxis: 'y',
            scales: {
                x: {
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(0,0,0,0.1)'
                    }
                },
                y: {
                    grid: {
                        display: false
                    }
                }
            },
            plugins: {
                ...commonOptions.plugins,
                legend: {
                    display: false
                }
            }
        }
    });
};

// Gauge chart for system health
window.updateGaugeChart = function(canvasId, value, maxValue = 100) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    // Destroy existing chart
    if (charts[canvasId]) {
        charts[canvasId].destroy();
    }

    const percentage = (value / maxValue) * 100;
    const color = percentage >= 80 ? '#4caf50' : percentage >= 60 ? '#ff9800' : '#f44336';

    charts[canvasId] = new Chart(ctx, {
        type: 'doughnut',
        data: {
            datasets: [{
                data: [percentage, 100 - percentage],
                backgroundColor: [color, 'rgba(0,0,0,0.1)'],
                borderWidth: 0,
                cutout: '75%'
            }]
        },
        options: {
            ...commonOptions,
            rotation: -90,
            circumference: 180,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    enabled: false
                }
            }
        },
        plugins: [{
            beforeDraw: function(chart) {
                const ctx = chart.ctx;
                const width = chart.width;
                const height = chart.height;
                
                ctx.restore();
                ctx.font = "bold 24px Arial";
                ctx.textBaseline = "middle";
                ctx.fillStyle = color;
                
                const text = `${percentage.toFixed(1)}%`;
                const textX = Math.round((width - ctx.measureText(text).width) / 2);
                const textY = height / 2 + 10;
                
                ctx.fillText(text, textX, textY);
                ctx.save();
            }
        }]
    });
};

// Real-time sparkline chart
window.createSparkline = function(canvasId, data, color = '#667eea') {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    // Destroy existing chart
    if (charts[canvasId]) {
        charts[canvasId].destroy();
    }

    charts[canvasId] = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map((_, i) => i),
            datasets: [{
                data: data,
                borderColor: color,
                backgroundColor: `${color}20`,
                borderWidth: 2,
                fill: true,
                pointRadius: 0,
                pointHoverRadius: 0,
                tension: 0.4
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: { enabled: false }
            },
            scales: {
                x: { display: false },
                y: { display: false }
            },
            elements: {
                point: { radius: 0 }
            },
            animation: { duration: 0 }
        }
    });
};

// Update sparkline with new data point
window.updateSparkline = function(canvasId, newValue) {
    const chart = charts[canvasId];
    if (!chart) return;

    const data = chart.data.datasets[0].data;
    data.push(newValue);
    
    // Keep only last 20 points
    if (data.length > 20) {
        data.shift();
    }
    
    chart.update('none');
};

// Heat map for error distribution
window.updateHeatMap = function(canvasId, data) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    // Destroy existing chart
    if (charts[canvasId]) {
        charts[canvasId].destroy();
    }

    // Transform data for heat map
    const datasets = data.map((row, rowIndex) => ({
        label: row.label,
        data: row.data.map((value, colIndex) => ({
            x: colIndex,
            y: rowIndex,
            v: value
        })),
        backgroundColor: function(context) {
            const value = context.parsed.v;
            const alpha = Math.min(value / 100, 1); // Normalize to 0-1
            return `rgba(244, 67, 54, ${alpha})`;
        }
    }));

    charts[canvasId] = new Chart(ctx, {
        type: 'scatter',
        data: { datasets },
        options: {
            ...commonOptions,
            scales: {
                x: {
                    type: 'linear',
                    position: 'bottom',
                    grid: { display: false }
                },
                y: {
                    type: 'linear',
                    grid: { display: false }
                }
            },
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        title: () => '',
                        label: function(context) {
                            return `Value: ${context.parsed.v}`;
                        }
                    }
                }
            },
            elements: {
                point: {
                    radius: 10,
                    hoverRadius: 12
                }
            }
        }
    });
};

// Polar area chart for environment distribution
window.updatePolarChart = function(canvasId, data) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    // Destroy existing chart
    if (charts[canvasId]) {
        charts[canvasId].destroy();
    }

    charts[canvasId] = new Chart(ctx, {
        type: 'polarArea',
        data: {
            labels: data.map(item => item.label),
            datasets: [{
                data: data.map(item => item.value),
                backgroundColor: data.map(item => item.color),
                borderWidth: 2,
                borderColor: '#fff'
            }]
        },
        options: {
            ...commonOptions,
            scales: {
                r: {
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(0,0,0,0.1)'
                    },
                    pointLabels: {
                        font: {
                            size: 12
                        }
                    }
                }
            }
        }
    });
};

// Cleanup function
window.destroyChart = function(canvasId) {
    if (charts[canvasId]) {
        charts[canvasId].destroy();
        delete charts[canvasId];
    }
};

// Destroy all charts
window.destroyAllCharts = function() {
    Object.keys(charts).forEach(chartId => {
        charts[chartId].destroy();
    });
    charts = {};
};

// Real-time data simulation for demo
window.startRealtimeDemo = function() {
    setInterval(() => {
        // Simulate real-time metrics updates
        const event = new CustomEvent('metricsUpdate', {
            detail: {
                logsPerSecond: Math.floor(Math.random() * 1000) + 500,
                errorRate: Math.random() * 5,
                throughput: Math.floor(Math.random() * 50) + 950
            }
        });
        window.dispatchEvent(event);

        // Simulate new log entries
        const levels = ['INFO', 'WARN', 'ERROR', 'DEBUG'];
        const apps = ['WebApp', 'API', 'Worker', 'Database'];
        const envs = ['Production', 'Staging', 'Development'];
        
        const logEvent = new CustomEvent('newLogEntry', {
            detail: {
                timestamp: new Date(),
                level: levels[Math.floor(Math.random() * levels.length)],
                application: apps[Math.floor(Math.random() * apps.length)],
                environment: envs[Math.floor(Math.random() * envs.length)],
                message: `Sample log message ${Math.floor(Math.random() * 1000)}`
            }
        });
        window.dispatchEvent(logEvent);
    }, 2000);
};

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        updateLineChart,
        updateDoughnutChart,
        updateBarChart,
        updateGaugeChart,
        createSparkline,
        updateSparkline,
        updateHeatMap,
        updatePolarChart,
        destroyChart,
        destroyAllCharts
    };
}