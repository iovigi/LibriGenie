import { extractUserFromRequest, requireRole } from '../../../services/auth/auth-middleware';

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

        // Check if user has admin role
        if (!requireRole(user, 'admin')) {
            return res.status(403).json({ 
                ok: false, 
                error: 'Access denied. Admin role required.',
                userRole: user.role 
            });
        }

        // Admin-only functionality here
        res.status(200).json({ 
            ok: true, 
            message: 'Admin access granted',
            adminInfo: {
                userId: user.userId,
                email: user.email,
                role: user.role,
                permissions: user.permissions
            }
        });
    } catch (error) {
        console.error('Admin access error:', error);
        res.status(500).json({ ok: false, error: 'Internal server error' });
    }
}
