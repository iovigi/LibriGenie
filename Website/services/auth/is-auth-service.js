import { verifyAccessToken, verifyRefreshToken } from "./token-generation";

export async function IsAuth(token, tokenType = 'access') {
    try {
        if (!token) {
            return false;
        }

        let verificationResult;
        
        if (tokenType === 'refresh') {
            verificationResult = await verifyRefreshToken(token);
        } else {
            verificationResult = await verifyAccessToken(token);
        }
        
        if (!verificationResult) {
            return false;
        }

        return !verificationResult.error;
    } catch (error) {
        return false;
    }
}