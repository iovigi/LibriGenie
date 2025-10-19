import crypto from 'crypto';

const csrfTokens = new Map();
const TOKEN_EXPIRY = 60 * 60 * 1000; // 1 hour

export function generateCSRFToken(sessionId) {
    const token = crypto.randomBytes(32).toString('hex');
    const expiry = Date.now() + TOKEN_EXPIRY;
    
    csrfTokens.set(token, {
        sessionId,
        expiry,
        used: false
    });
    
    return token;
}

export function validateCSRFToken(token, sessionId, markAsUsed = true) {
    console.log(`CSRF validation: token=${token.substring(0, 8)}..., session=${sessionId.substring(0, 8)}..., markAsUsed=${markAsUsed}`);
    
    if (!token || !sessionId) {
        console.log('CSRF validation failed: missing token or session');
        return false;
    }
    
    const tokenData = csrfTokens.get(token);
    
    if (!tokenData) {
        console.log('CSRF validation failed: token not found in store');
        return false;
    }
    
    // Check if token has expired
    if (Date.now() > tokenData.expiry) {
        console.log('CSRF validation failed: token expired');
        csrfTokens.delete(token);
        return false;
    }
    
    // Check if token belongs to the session
    if (tokenData.sessionId !== sessionId) {
        console.log(`CSRF validation failed: session mismatch (expected: ${tokenData.sessionId.substring(0, 8)}..., got: ${sessionId.substring(0, 8)}...)`);
        return false;
    }
    
    // For authentication endpoints, allow token reuse within a short time window
    // This prevents issues with form validation and submission
    if (markAsUsed && tokenData.used) {
        // Allow reuse if token was used less than 2 minutes ago
        const timeSinceUsed = Date.now() - (tokenData.lastUsed || 0);
        if (timeSinceUsed > 120000) { // 2 minutes
            console.log(`CSRF token reuse denied (used ${Math.round(timeSinceUsed/1000)}s ago, limit: 120s)`);
            return false;
        }
        console.log(`CSRF token reuse allowed (used ${Math.round(timeSinceUsed/1000)}s ago)`);
    }
    
    // Mark token as used if requested
    if (markAsUsed) {
        tokenData.used = true;
        tokenData.lastUsed = Date.now();
        console.log('CSRF token marked as used');
    }
    
    console.log('CSRF validation successful');
    return true;
}

export function checkCSRFToken(token, sessionId) {
    return validateCSRFToken(token, sessionId, false);
}

export function refreshCSRFToken(sessionId) {
    // Generate a new token for the same session
    return generateCSRFToken(sessionId);
}

export function generateNewTokenForSession(sessionId) {
    // Remove any existing tokens for this session
    for (const [token, data] of csrfTokens.entries()) {
        if (data.sessionId === sessionId) {
            csrfTokens.delete(token);
        }
    }
    
    // Generate a new token
    return generateCSRFToken(sessionId);
}

export function cleanupExpiredTokens() {
    const now = Date.now();
    for (const [token, data] of csrfTokens.entries()) {
        if (now > data.expiry) {
            csrfTokens.delete(token);
        }
    }
}

// Clean up expired tokens every 30 minutes
setInterval(cleanupExpiredTokens, 30 * 60 * 1000);
