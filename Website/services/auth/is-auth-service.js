import { verifyAccessToken } from "./token-generation";

export async function IsAuth(token) {
    try {
        if (!token) {
            return false;
        }

        const accessToken = await verifyAccessToken(token);
        
        if (!accessToken) {
            return false;
        }

        return !accessToken.error;
    } catch (error) {
        return false;
    }
}