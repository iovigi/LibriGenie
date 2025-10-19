export function validatePassword(password) {
    const errors = [];
    
    // Minimum length
    if (password.length < 8) {
        errors.push("Password must be at least 8 characters long");
    }
    
    // Maximum length
    if (password.length > 128) {
        errors.push("Password must be less than 128 characters");
    }
    
    // At least one uppercase letter
    if (!/[A-Z]/.test(password)) {
        errors.push("Password must contain at least one uppercase letter");
    }
    
    // At least one lowercase letter
    if (!/[a-z]/.test(password)) {
        errors.push("Password must contain at least one lowercase letter");
    }
    
    // At least one number
    if (!/\d/.test(password)) {
        errors.push("Password must contain at least one number");
    }
    
    // At least one special character
    if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)) {
        errors.push("Password must contain at least one special character");
    }
    
    // Check for repeated characters (3 or more) - handled separately
    if (/(.)\1{2,}/.test(password)) {
        errors.push("Password cannot contain 3 or more repeated characters");
    }
    
    // Check for other common patterns
    const commonPatterns = [
        /123|234|345|456|567|678|789|890/, // Sequential numbers
        /abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|vwx|wxy|xyz/, // Sequential letters
        /password|123456|qwerty|admin|letmein/i // Common passwords
    ];
    
    for (const pattern of commonPatterns) {
        if (pattern.test(password.toLowerCase())) {
            errors.push("Password contains common patterns and is not secure");
            break;
        }
    }
    
    return {
        isValid: errors.length === 0,
        errors: errors
    };
}

export function validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return {
        isValid: emailRegex.test(email),
        error: emailRegex.test(email) ? null : "Please enter a valid email address"
    };
}
