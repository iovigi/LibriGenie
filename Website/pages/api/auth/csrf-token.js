import { generateCSRFToken } from '../../../services/auth/csrf-protection';
import { setCookie } from 'cookies-next';

export default async function handler(req, res) {
    if (req.method !== 'GET') {
        return res.status(405).json({ ok: false, error: 'Method not allowed' });
    }

    try {
        // Generate a session ID if not exists
        let sessionId = req.cookies.sessionId;
        if (!sessionId) {
            sessionId = require('crypto').randomBytes(32).toString('hex');
            setCookie('sessionId', sessionId, {
                req,
                res,
                maxAge: 60 * 60 * 24, // 24 hours
                httpOnly: true,
                secure: process.env.NODE_ENV === 'production',
                sameSite: 'strict'
            });
            console.log('Created new session:', sessionId.substring(0, 8) + '...');
        } else {
            console.log('Using existing session:', sessionId.substring(0, 8) + '...');
        }

        // Generate CSRF token
        const csrfToken = generateCSRFToken(sessionId);

        console.log('Generated CSRF token for session:', sessionId.substring(0, 8) + '...');

        res.status(200).json({ 
            ok: true, 
            csrfToken: csrfToken,
            sessionId: sessionId.substring(0, 8) + '...' // For debugging
        });
    } catch (error) {
        console.error('CSRF token generation error:', error);
        res.status(500).json({ ok: false, error: 'Internal server error' });
    }
}
