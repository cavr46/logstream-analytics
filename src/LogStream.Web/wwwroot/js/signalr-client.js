// SignalR client for real-time updates
window.logStreamSignalR = {
    connection: null,
    isConnected: false,
    
    // Initialize SignalR connection
    init: async () => {
        try {
            window.logStreamSignalR.connection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/logstream")
                .withAutomaticReconnect()
                .build();

            // Set up event handlers
            window.logStreamSignalR.setupEventHandlers();

            // Start the connection
            await window.logStreamSignalR.connection.start();
            window.logStreamSignalR.isConnected = true;
            console.log('SignalR connected successfully');

            return true;
        } catch (err) {
            console.error('SignalR connection failed:', err);
            window.logStreamSignalR.isConnected = false;
            return false;
        }
    },

    // Set up event handlers
    setupEventHandlers: () => {
        const connection = window.logStreamSignalR.connection;
        if (!connection) return;

        // Handle new log entries
        connection.on("NewLogEntry", (logEntry) => {
            window.logStreamSignalR.onNewLogEntry(logEntry);
        });

        // Handle metrics updates
        connection.on("MetricsUpdate", (metrics) => {
            window.logStreamSignalR.onMetricsUpdate(metrics);
        });

        // Handle new alerts
        connection.on("NewAlert", (alert) => {
            window.logStreamSignalR.onNewAlert(alert);
        });

        // Handle system status updates
        connection.on("SystemStatus", (status) => {
            window.logStreamSignalR.onSystemStatus(status);
        });

        // Handle connection state changes
        connection.onreconnecting((error) => {
            console.log('SignalR reconnecting:', error);
            window.logStreamSignalR.isConnected = false;
        });

        connection.onreconnected((connectionId) => {
            console.log('SignalR reconnected:', connectionId);
            window.logStreamSignalR.isConnected = true;
        });

        connection.onclose((error) => {
            console.log('SignalR connection closed:', error);
            window.logStreamSignalR.isConnected = false;
        });
    },

    // Join a tenant group for receiving updates
    joinTenantGroup: async (tenantId) => {
        if (!window.logStreamSignalR.isConnected || !window.logStreamSignalR.connection) {
            console.warn('SignalR not connected, cannot join tenant group');
            return false;
        }

        try {
            await window.logStreamSignalR.connection.invoke("JoinTenantGroup", tenantId);
            console.log(`Joined tenant group: ${tenantId}`);
            return true;
        } catch (err) {
            console.error('Failed to join tenant group:', err);
            return false;
        }
    },

    // Leave a tenant group
    leaveTenantGroup: async (tenantId) => {
        if (!window.logStreamSignalR.isConnected || !window.logStreamSignalR.connection) {
            console.warn('SignalR not connected, cannot leave tenant group');
            return false;
        }

        try {
            await window.logStreamSignalR.connection.invoke("LeaveTenantGroup", tenantId);
            console.log(`Left tenant group: ${tenantId}`);
            return true;
        } catch (err) {
            console.error('Failed to leave tenant group:', err);
            return false;
        }
    },

    // Event handlers (can be overridden by pages)
    onNewLogEntry: (logEntry) => {
        console.log('New log entry received:', logEntry);
        
        // Dispatch custom event for components to listen to
        window.dispatchEvent(new CustomEvent('newLogEntry', { 
            detail: logEntry 
        }));

        // Show notification for critical logs
        if (logEntry.level === 'ERROR' || logEntry.level === 'FATAL') {
            window.logStreamSignalR.showNotification(
                'Critical Log Entry',
                `${logEntry.level}: ${logEntry.message.substring(0, 100)}...`,
                'error'
            );
        }
    },

    onMetricsUpdate: (metrics) => {
        console.log('Metrics update received:', metrics);
        
        // Dispatch custom event for components to listen to
        window.dispatchEvent(new CustomEvent('metricsUpdate', { 
            detail: metrics 
        }));
    },

    onNewAlert: (alert) => {
        console.log('New alert received:', alert);
        
        // Show notification
        window.logStreamSignalR.showNotification(
            alert.title || 'New Alert',
            alert.message,
            alert.severity?.toLowerCase() || 'warning'
        );

        // Dispatch custom event for components to listen to
        window.dispatchEvent(new CustomEvent('newAlert', { 
            detail: alert 
        }));
    },

    onSystemStatus: (status) => {
        console.log('System status update received:', status);
        
        // Dispatch custom event for components to listen to
        window.dispatchEvent(new CustomEvent('systemStatus', { 
            detail: status 
        }));
    },

    // Show browser notification
    showNotification: (title, message, type = 'info') => {
        // Check if notifications are supported and permitted
        if (!("Notification" in window)) {
            console.warn('Browser notifications not supported');
            return;
        }

        if (Notification.permission === "granted") {
            const notification = new Notification(title, {
                body: message,
                icon: '/favicon.png',
                tag: 'logstream-' + Date.now()
            });

            // Auto-close after 5 seconds
            setTimeout(() => notification.close(), 5000);
        } else if (Notification.permission !== "denied") {
            // Request permission
            Notification.requestPermission().then((permission) => {
                if (permission === "granted") {
                    window.logStreamSignalR.showNotification(title, message, type);
                }
            });
        }
    },

    // Disconnect SignalR
    disconnect: async () => {
        if (window.logStreamSignalR.connection) {
            try {
                await window.logStreamSignalR.connection.stop();
                console.log('SignalR disconnected');
            } catch (err) {
                console.error('Error disconnecting SignalR:', err);
            }
        }
        window.logStreamSignalR.isConnected = false;
    }
};

// Auto-initialize SignalR when the page loads
document.addEventListener('DOMContentLoaded', () => {
    // Wait a bit for Blazor to initialize
    setTimeout(() => {
        window.logStreamSignalR.init();
    }, 1000);
});

// Clean up on page unload
window.addEventListener('beforeunload', () => {
    window.logStreamSignalR.disconnect();
});