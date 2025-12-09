// Shared authentication utility
class Auth {
    static getToken() {
        return localStorage.getItem('token');
    }

    static isAuthenticated() {
        return !!this.getToken();
    }

    static getUserEmail() {
        return localStorage.getItem('userEmail');
    }

    static getUserFirstName() {
        return localStorage.getItem('userFirstName');
    }

    static getUserLastName() {
        return localStorage.getItem('userLastName');
    }

    static getUserDisplayName() {
        const firstName = this.getUserFirstName();
        const lastName = this.getUserLastName();
        if (firstName && lastName) {
            return `${firstName} ${lastName}`;
        }
        return this.getUserEmail() || 'User';
    }

    static logout() {
        localStorage.removeItem('token');
        localStorage.removeItem('userEmail');
        localStorage.removeItem('userFirstName');
        localStorage.removeItem('userLastName');
        localStorage.removeItem('encryptionSalt');
        
        // Clear encryption keys and API keys from memory
        if (window.cryptoManager) {
            window.cryptoManager.clear();
        }
        
        // Clear all API keys from sessionStorage
        for (let i = sessionStorage.length - 1; i >= 0; i--) {
            const key = sessionStorage.key(i);
            if (key && key.startsWith('_apik_')) {
                sessionStorage.removeItem(key);
            }
        }
        
        window.location.href = '/login.html';
    }

    static requireAuth() {
        if (!this.isAuthenticated()) {
            window.location.href = '/login.html';
            return false;
        }
        return true;
    }

    static async fetchWithAuth(url, options = {}) {
        const token = this.getToken();
        if (!token) {
            this.logout();
            throw new Error('Not authenticated');
        }

        const headers = {
            ...options.headers,
            'Authorization': `Bearer ${token}`
        };

        const response = await fetch(url, {
            ...options,
            headers
        });

        // If unauthorized, logout and redirect
        if (response.status === 401) {
            this.logout();
            throw new Error('Session expired');
        }

        return response;
    }
}

// Initialize auth check on page load
document.addEventListener('DOMContentLoaded', () => {
    // Skip auth check on public pages
    const publicPages = ['/login.html', '/register.html', '/landing.html', '/index.html', '/'];
    const currentPath = window.location.pathname;
    
    if (!publicPages.includes(currentPath) && !publicPages.some(page => currentPath.endsWith(page))) {
        Auth.requireAuth();
    }
});
