// Simple in-memory rate limiter (for production, consider Redis)
const attempts = new Map();
const maxAttempts = 5;
const windowMs = 15 * 60 * 1000; // 15 minutes

export function checkRateLimit(identifier, maxAttemptsPerWindow = maxAttempts, windowSizeMs = windowMs) {
    const now = Date.now();
    const key = `rate_limit_${identifier}`;
    
    if (!attempts.has(key)) {
        attempts.set(key, { count: 0, resetTime: now + windowSizeMs });
    }
    
    const attemptData = attempts.get(key);
    
    // Reset if window has expired
    if (now > attemptData.resetTime) {
        attemptData.count = 0;
        attemptData.resetTime = now + windowSizeMs;
    }
    
    // Check if limit exceeded
    if (attemptData.count >= maxAttemptsPerWindow) {
        const remainingTime = Math.ceil((attemptData.resetTime - now) / 1000 / 60);
        return {
            allowed: false,
            remainingTime: remainingTime,
            message: `Too many attempts. Try again in ${remainingTime} minutes.`
        };
    }
    
    // Increment attempt count
    attemptData.count++;
    
    return {
        allowed: true,
        remainingAttempts: maxAttemptsPerWindow - attemptData.count
    };
}

export function resetRateLimit(identifier) {
    const key = `rate_limit_${identifier}`;
    attempts.delete(key);
}

// Clean up expired entries periodically
setInterval(() => {
    const now = Date.now();
    for (const [key, data] of attempts.entries()) {
        if (now > data.resetTime) {
            attempts.delete(key);
        }
    }
}, 5 * 60 * 1000); // Clean up every 5 minutes
