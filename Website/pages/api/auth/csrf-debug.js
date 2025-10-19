import { validateCSRFToken, checkCSRFToken } from '../../../services/auth/csrf-protection';

export default async function handler(req, res) {
    if (req.method !== 'GET') {
        return res.status(405).json({ ok: false, error: 'Method not allowed' });
    }

    try {
        const csrfToken = req.query.token;
        const sessionId = req.cookies.sessionId;

        console.log('CSRF Debug - Token:', csrfToken ? csrfToken.substring(0, 8) + '...' : 'none');
        console.log('CSRF Debug - Session:', sessionId ? sessionId.substring(0, 8) + '...' : 'none');
        console.log('CSRF Debug - All cookies:', req.cookies);

        if (!csrfToken || !sessionId) {
            return res.status(400).json({ 
                ok: false, 
                error: 'Missing CSRF token or session ID',
                hasToken: !!csrfToken,
                hasSession: !!sessionId,
                cookies: Object.keys(req.cookies)
            });
        }

        // Test without marking as used
        const isValid = checkCSRFToken(csrfToken, sessionId);
        
        res.status(200).json({ 
            ok: true, 
            isValid: isValid,
            tokenPreview: csrfToken.substring(0, 8) + '...',
            sessionPreview: sessionId.substring(0, 8) + '...',
            allCookies: Object.keys(req.cookies)
        });
    } catch (error) {
        console.error('CSRF debug error:', error);
        res.status(500).json({ ok: false, error: 'Internal server error' });
    }
}
