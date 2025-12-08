// Client-side encryption/decryption utilities
// Uses Web Crypto API for zero-knowledge encryption

class CryptoManager {
    constructor() {
        this.encryptionKey = null;
        this.salt = null; // Will be user-specific, stored on server
        this.isInitialized = false;
    }

    // Derive encryption key from password using PBKDF2
    async deriveKey(password, salt) {
        const encoder = new TextEncoder();
        const passwordBuffer = encoder.encode(password);
        
        // Import password as key material
        const keyMaterial = await crypto.subtle.importKey(
            'raw',
            passwordBuffer,
            'PBKDF2',
            false,
            ['deriveKey']
        );

        // Derive AES-GCM key
        const key = await crypto.subtle.deriveKey(
            {
                name: 'PBKDF2',
                salt: salt,
                iterations: 100000,
                hash: 'SHA-256'
            },
            keyMaterial,
            { name: 'AES-GCM', length: 256 },
            false,
            ['encrypt', 'decrypt']
        );

        return key;
    }

    // Initialize crypto manager with user's password
    async initialize(password, userSalt) {
        // Convert base64 salt to Uint8Array
        const saltBuffer = Uint8Array.from(atob(userSalt), c => c.charCodeAt(0));
        this.salt = saltBuffer;
        this.encryptionKey = await this.deriveKey(password, saltBuffer);
        this.isInitialized = true;
        console.log('CryptoManager initialized successfully');
    }

    // Generate random salt for new users
    generateSalt() {
        const salt = crypto.getRandomValues(new Uint8Array(16));
        return btoa(String.fromCharCode(...salt));
    }

    // Encrypt a string value
    async encrypt(plaintext) {
        if (!this.encryptionKey) {
            throw new Error('Encryption key not initialized');
        }

        if (plaintext === null || plaintext === undefined || plaintext === '') {
            return null;
        }

        const encoder = new TextEncoder();
        const data = encoder.encode(plaintext);
        
        // Generate random IV for each encryption
        const iv = crypto.getRandomValues(new Uint8Array(12));
        
        const encrypted = await crypto.subtle.encrypt(
            { name: 'AES-GCM', iv: iv },
            this.encryptionKey,
            data
        );

        // Combine IV and encrypted data
        const combined = new Uint8Array(iv.length + encrypted.byteLength);
        combined.set(iv, 0);
        combined.set(new Uint8Array(encrypted), iv.length);

        // Return base64 encoded
        return btoa(String.fromCharCode(...combined));
    }

    // Decrypt a string value
    async decrypt(ciphertext) {
        if (!this.encryptionKey) {
            throw new Error('Encryption key not initialized');
        }

        if (ciphertext === null || ciphertext === undefined || ciphertext === '') {
            return null;
        }

        try {
            // Decode base64
            const combined = Uint8Array.from(atob(ciphertext), c => c.charCodeAt(0));
            
            // Extract IV and encrypted data
            const iv = combined.slice(0, 12);
            const data = combined.slice(12);

            const decrypted = await crypto.subtle.decrypt(
                { name: 'AES-GCM', iv: iv },
                this.encryptionKey,
                data
            );

            const decoder = new TextDecoder();
            return decoder.decode(decrypted);
        } catch (error) {
            console.error('Decryption failed:', error);
            throw new Error('Failed to decrypt data. Wrong password?');
        }
    }

    // Encrypt a number (convert to string first)
    async encryptNumber(number) {
        if (number === null || number === undefined) {
            return null;
        }
        return await this.encrypt(number.toString());
    }

    // Decrypt a number (convert back from string)
    async decryptNumber(ciphertext) {
        const decrypted = await this.decrypt(ciphertext);
        return decrypted ? parseFloat(decrypted) : null;
    }

    // Encrypt a transaction object
    async encryptTransaction(transaction) {
        return {
            ...transaction,
            description: await this.encrypt(transaction.description),
            counterpartyName: await this.encrypt(transaction.counterpartyName),
            amount: await this.encryptNumber(transaction.amount),
            category: await this.encrypt(transaction.category),
            // Keep these unencrypted for server-side operations
            id: transaction.id,
            transactionDate: transaction.transactionDate,
            financialAccountId: transaction.financialAccountId,
            transactionId: transaction.transactionId,
            isEssentialExpense: transaction.isEssentialExpense,
            createdAt: transaction.createdAt
        };
    }

    // Decrypt a transaction object
    async decryptTransaction(transaction) {
        return {
            ...transaction,
            description: await this.decrypt(transaction.description),
            counterpartyName: await this.decrypt(transaction.counterpartyName),
            amount: await this.decryptNumber(transaction.amount),
            category: await this.decrypt(transaction.category)
        };
    }

    // Decrypt multiple transactions
    async decryptTransactions(transactions) {
        return await Promise.all(
            transactions.map(t => this.decryptTransaction(t))
        );
    }

    // Clear encryption key from memory
    clear() {
        this.encryptionKey = null;
        this.salt = null;
    }
}

// Global instance
window.cryptoManager = new CryptoManager();
