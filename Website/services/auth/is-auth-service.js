import { verifyAccessToken } from "./token-generation";

export async function IsAuth(token) {
    var accessToken = await verifyAccessToken(token);

    return accessToken && !accessToken.error;
}