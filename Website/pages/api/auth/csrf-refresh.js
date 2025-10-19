import { generateNewTokenForSession } from '../../../services/auth/csrf-protection';

export default async function handler(req, res) {
    if (req.method !== 'POST') {
        return res.status(405).json({ ok: false, error: 'Method not allowed' });
    }

    try {
        const sessionId = req.cookies.sessionId;

        if (!sessionId) {
            return res.status(400).json({ 
                ok: false, 
                error: 'No session found. Please refresh the page.' 
            });
        }

        // Generate a new CSRF token for the same session
        const newCsrfToken = generateNewTokenForSession(sessionId);

        console.log('Refreshed CSRF token for session:', sessionId.substring(0, 8) + '...');

        res.status(200).json({ 
            ok: true, 
            csrfToken: newCsrfToken,
            sessionId: sessionId.substring(0, 8) + '...' // For debugging
        });
    } catch (error) {
        console.error('CSRF token refresh error:', error);
        res.status(500).json({ ok: false, error: 'Internal server error' });
    }
}
