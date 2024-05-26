
import { decodeJwt, errors, jwtVerify, SignJWT } from "jose";

export function generateAccessToken(userId) {
    const jwt = new SignJWT()
        .setProtectedHeader({ alg: "HS256" })
        .setIssuedAt()
        .setExpirationTime("300s")
        .setSubject(userId)
        .sign(new TextEncoder().encode("SecretKey"));

    return jwt;
}

export async function verifyAccessToken(accessToken) {
    try {
        const payload = await jwtVerify(
            accessToken,
            new TextEncoder().encode("SecretKey")
        );
        if (!payload.payload.sub)
            return {
                error: "INVALID",
                message: "Invalid token",
            };

        return { accessToken, userId: payload.payload.sub };
    } catch (err) {
        if (err instanceof errors.JWTExpired)
            return {
                error: "EXPIRED",
                message: "Token has expired",
            };

        return null;
    }
}