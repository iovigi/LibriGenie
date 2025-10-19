import { getUserFromAccessToken, getUserFromRefreshToken } from './token-utils';

/**
 * Middleware to extract user information from tokens
 * @param {Object} req - Request object
 * @returns {Object} User information or null if not authenticated
 */
export async function extractUserFromRequest(req) {
    try {
        const accessToken = req.cookies.accessToken;
        const refreshToken = req.cookies.refreshToken;

        if (!accessToken && !refreshToken) {
            return null;
        }

        let user = null;

        // Try access token first
        if (accessToken) {
            user = await getUserFromAccessToken(accessToken);
        }

        // If access token failed, try refresh token
        if (!user && refreshToken) {
            user = await getUserFromRefreshToken(refreshToken);
        }

        return user;
    } catch (error) {
        console.error('Error extracting user from request:', error);
        return null;
    }
}

/**
 * Middleware to check if user has specific role
 * @param {Object} user - User object from extractUserFromRequest
 * @param {string} requiredRole - Required role
 * @returns {boolean} Whether user has the required role
 */
export function requireRole(user, requiredRole) {
    if (!user) {
        return false;
    }
    
    return user.role === requiredRole;
}

/**
 * Middleware to check if user has specific permission
 * @param {Object} user - User object from extractUserFromRequest
 * @param {string} permission - Required permission
 * @returns {boolean} Whether user has the required permission
 */
export function requirePermission(user, permission) {
    if (!user) {
        return false;
    }
    
    return user.permissions && user.permissions.includes(permission);
}

/**
 * Middleware to check if user account is active
 * @param {Object} user - User object from extractUserFromRequest
 * @returns {boolean} Whether user account is active
 */
export function requireActiveAccount(user) {
    if (!user) {
        return false;
    }
    
    return user.accountStatus === 'active';
}
