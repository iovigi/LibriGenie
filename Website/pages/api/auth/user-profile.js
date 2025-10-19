import { extractUserFromRequest, requireActiveAccount } from '../../../services/auth/auth-middleware';

export default async function handler(req, res) {
    if (req.method !== 'GET') {
        return res.status(405).json({ ok: false, error: 'Method not allowed' });
    }

    try {
        // Extract user information from token
        const user = await extractUserFromRequest(req);
        
        if (!user) {
            return res.status(401).json({ ok: false, error: 'Not authenticated' });
        }

        // Check if account is active
        if (!requireActiveAccount(user)) {
            return res.status(403).json({ ok: false, error: 'Account is not active' });
        }

        // Return user profile with custom claims
        res.status(200).json({ 
            ok: true, 
            profile: {
                userId: user.userId,
                email: user.email,
                role: user.role,
                permissions: user.permissions,
                lastLogin: user.lastLogin,
                accountStatus: user.accountStatus,
                tokenInfo: {
                    tokenId: user.tokenId,
                    issuedAt: new Date(user.issuedAt * 1000).toISOString(),
                    expiresAt: new Date(user.expiresAt * 1000).toISOString()
                }
            }
        });
    } catch (error) {
        console.error('Get user profile error:', error);
        res.status(500).json({ ok: false, error: 'Internal server error' });
    }
}
