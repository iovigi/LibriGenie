export default async function handler(req, res) {
    if (req.method !== 'GET') {
        return res.status(405).json({ ok: false, error: 'Method not allowed' });
    }

    try {
        // Get all cookies
        const cookies = req.cookies;
        const sessionId = cookies.sessionId;
        
        console.log('All cookies received:', Object.keys(cookies));
        console.log('Session ID:', sessionId ? sessionId.substring(0, 8) + '...' : 'none');
        
        res.status(200).json({ 
            ok: true, 
            cookies: Object.keys(cookies),
            hasSession: !!sessionId,
            sessionPreview: sessionId ? sessionId.substring(0, 8) + '...' : null
        });
    } catch (error) {
        console.error('CSRF test error:', error);
        res.status(500).json({ ok: false, error: 'Internal server error' });
    }
}
