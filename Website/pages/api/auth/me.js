import { getUserFromAccessToken, getUserFromRefreshToken } from '../../../services/auth/token-utils';

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

        let user = null;

        // Try access token first
        if (accessToken) {
            user = await getUserFromAccessToken(accessToken);
        }

        // If access token failed, try refresh token
        if (!user && refreshToken) {
            user = await getUserFromRefreshToken(refreshToken);
        }

        if (!user) {
            return res.status(401).json({ ok: false, error: 'Invalid or expired tokens' });
        }

        // Return user information (excluding sensitive data)
        res.status(200).json({ 
            ok: true, 
            user: {
                userId: user.userId,
                email: user.email,
                role: user.role,
                permissions: user.permissions,
                lastLogin: user.lastLogin,
                accountStatus: user.accountStatus
            }
        });
    } catch (error) {
        console.error('Get user info error:', error);
        res.status(500).json({ ok: false, error: 'Internal server error' });
    }
}
