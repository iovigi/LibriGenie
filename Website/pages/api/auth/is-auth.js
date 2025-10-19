import { IsAuth } from "../../../services/auth/is-auth-service"

export default async function handler(req, res) {
    if (req.method !== 'GET') {
        return res.status(405).json({ ok: false, error: 'Method not allowed' });
    }

    try {
        const accessToken = req.cookies.accessToken;
        const refreshToken = req.cookies.refreshToken;

        if (!accessToken && !refreshToken) {
            return res.status(401).json({ ok: false, error: 'No authentication tokens' });
        }

        // Check access token first
        if (accessToken) {
            const isAccessTokenValid = await IsAuth(accessToken, 'access');
            if (isAccessTokenValid) {
                return res.status(200).json({ ok: true, error: "" });
            }
        }

        // Check refresh token if access token is invalid
        if (refreshToken) {
            const isRefreshTokenValid = await IsAuth(refreshToken, 'refresh');
            if (isRefreshTokenValid) {
                return res.status(200).json({ ok: true, error: "" });
            }
        }

        res.status(401).json({ ok: false, error: 'Invalid or expired tokens' });
    } catch (error) {
        console.error('Auth check error:', error);
        res.status(500).json({ ok: false, error: 'Internal server error' });
    }
}