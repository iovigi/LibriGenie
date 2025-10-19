
import { decodeJwt, errors, jwtVerify, SignJWT } from "jose";
import crypto from 'crypto';

export function generateAccessToken(userId, userClaims = {}) {
    const jwt = new SignJWT(userClaims) // Pass claims directly to constructor
        .setProtectedHeader({ alg: "HS256" })
        .setIssuedAt()
        .setExpirationTime("24h")
        .setSubject(userId)
        .setAudience("access")
        .setIssuer("LibriGenie")
        .setJti(crypto.randomUUID()); // Unique token ID

    return jwt.sign(new TextEncoder().encode(process.env.secret));
}

export function generateRefreshToken(userId, userClaims = {}) {
    const jwt = new SignJWT(userClaims) // Pass claims directly to constructor
        .setProtectedHeader({ alg: "HS256" })
        .setIssuedAt()
        .setExpirationTime("7d")
        .setSubject(userId)
        .setAudience("refresh")
        .setIssuer("LibriGenie")
        .setJti(crypto.randomUUID()); // Unique token ID

    return jwt.sign(new TextEncoder().encode(process.env.secret));
}

export async function verifyAccessToken(accessToken) {
    try {
        const payload = await jwtVerify(
            accessToken,
            new TextEncoder().encode(process.env.secret)
        );
        if (!payload.payload.sub)
            return {
                error: "INVALID",
                message: "Invalid token",
            };

        return { 
            accessToken, 
            userId: payload.payload.sub,
            claims: payload.payload // Include all custom claims
        };
    } catch (err) {
        if (err instanceof errors.JWTExpired)
            return {
                error: "EXPIRED",
                message: "Token has expired",
            };

        return null;
    }
}

export async function verifyRefreshToken(refreshToken) {
    try {
        const payload = await jwtVerify(
            refreshToken,
            new TextEncoder().encode(process.env.secret)
        );
        
        if (!payload.payload.sub || payload.payload.aud !== "refresh")
            return {
                error: "INVALID",
                message: "Invalid refresh token",
            };

        return { 
            refreshToken, 
            userId: payload.payload.sub,
            claims: payload.payload // Include all custom claims
        };
    } catch (err) {
        if (err instanceof errors.JWTExpired)
            return {
                error: "EXPIRED",
                message: "Refresh token has expired",
            };

        return null;
    }
}