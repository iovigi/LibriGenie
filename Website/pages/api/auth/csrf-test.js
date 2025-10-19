import { validateCSRFToken, checkCSRFToken } from '../../../services/auth/csrf-protection';

export default async function handler(req, res) {
    if (req.method !== 'POST') {
        return res.status(405).json({ ok: false, error: 'Method not allowed' });
    }

    try {
        const { csrfToken } = req.body;
        const sessionId = req.cookies.sessionId;

        console.log('CSRF Test - Token:', csrfToken ? csrfToken.substring(0, 8) + '...' : 'none');
        console.log('CSRF Test - Session:', sessionId ? sessionId.substring(0, 8) + '...' : 'none');

        if (!csrfToken || !sessionId) {
            return res.status(400).json({ 
                ok: false, 
                error: 'Missing CSRF token or session ID',
                hasToken: !!csrfToken,
                hasSession: !!sessionId
            });
        }

        // Test without marking as used
        const isValid = checkCSRFToken(csrfToken, sessionId);
        
        res.status(200).json({ 
            ok: true, 
            isValid: isValid,
            tokenPreview: csrfToken.substring(0, 8) + '...',
            sessionPreview: sessionId.substring(0, 8) + '...'
        });
    } catch (error) {
        console.error('CSRF test error:', error);
        res.status(500).json({ ok: false, error: 'Internal server error' });
    }
}
