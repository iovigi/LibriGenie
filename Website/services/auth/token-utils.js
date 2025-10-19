import { verifyAccessToken, verifyRefreshToken } from './token-generation';

/**
 * Extract user information from access token
 * @param {string} accessToken - The JWT access token
 * @returns {Object} User information from token claims
 */
export async function getUserFromAccessToken(accessToken) {
    try {
        const result = await verifyAccessToken(accessToken);
        
        if (!result || result.error) {
            return null;
        }

        return {
            userId: result.userId,
            email: result.claims.email,
            role: result.claims.role,
            permissions: result.claims.permissions,
            lastLogin: result.claims.lastLogin,
            accountStatus: result.claims.accountStatus,
            tokenId: result.claims.jti,
            issuedAt: result.claims.iat,
            expiresAt: result.claims.exp
        };
    } catch (error) {
        console.error('Error extracting user from access token:', error);
        return null;
    }
}

/**
 * Extract user information from refresh token
 * @param {string} refreshToken - The JWT refresh token
 * @returns {Object} User information from token claims
 */
export async function getUserFromRefreshToken(refreshToken) {
    try {
        const result = await verifyRefreshToken(refreshToken);
        
        if (!result || result.error) {
            return null;
        }

        return {
            userId: result.userId,
            email: result.claims.email,
            role: result.claims.role,
            permissions: result.claims.permissions,
            lastLogin: result.claims.lastLogin,
            accountStatus: result.claims.accountStatus,
            tokenId: result.claims.jti,
            issuedAt: result.claims.iat,
            expiresAt: result.claims.exp
        };
    } catch (error) {
        console.error('Error extracting user from refresh token:', error);
        return null;
    }
}

/**
 * Check if user has specific permission
 * @param {Object} userClaims - User claims from token
 * @param {string} permission - Permission to check
 * @returns {boolean} Whether user has the permission
 */
export function hasPermission(userClaims, permission) {
    if (!userClaims || !userClaims.permissions) {
        return false;
    }
    
    return userClaims.permissions.includes(permission);
}

/**
 * Check if user has specific role
 * @param {Object} userClaims - User claims from token
 * @param {string} role - Role to check
 * @returns {boolean} Whether user has the role
 */
export function hasRole(userClaims, role) {
    if (!userClaims || !userClaims.role) {
        return false;
    }
    
    return userClaims.role === role;
}

/**
 * Check if user account is active
 * @param {Object} userClaims - User claims from token
 * @returns {boolean} Whether user account is active
 */
export function isAccountActive(userClaims) {
    if (!userClaims || !userClaims.accountStatus) {
        return false;
    }
    
    return userClaims.accountStatus === 'active';
}
