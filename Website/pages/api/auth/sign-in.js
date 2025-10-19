import { SignIn } from '../../../services/auth/auth-service'
import { setCookie, deleteCookie, hasCookie, getCookie, getCookies } from 'cookies-next';
import { checkRateLimit, resetRateLimit } from '../../../services/auth/rate-limiter';
import { validateCSRFToken } from '../../../services/auth/csrf-protection';

export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ ok: false, error: 'Method not allowed' });
  }

  const { email, password, csrfToken } = req.body;
  
  if (!email || !password) {
    return res.status(400).json({ ok: false, error: 'Email and password are required' });
  }

  // Validate CSRF token
  const sessionId = req.cookies.sessionId;
  console.log('CSRF validation - Token:', csrfToken ? csrfToken.substring(0, 8) + '...' : 'none');
  console.log('CSRF validation - Session:', sessionId ? sessionId.substring(0, 8) + '...' : 'none');
  
  if (!validateCSRFToken(csrfToken, sessionId)) {
    console.log('CSRF token validation failed');
    return res.status(403).json({ ok: false, error: 'Invalid CSRF token' });
  }
  
  console.log('CSRF token validation successful');

  // Get client IP for rate limiting
  const clientIP = req.headers['x-forwarded-for'] || req.connection.remoteAddress || 'unknown';
  
  // Check rate limit
  const rateLimitResult = checkRateLimit(clientIP);
  if (!rateLimitResult.allowed) {
    return res.status(429).json({ 
      ok: false, 
      error: rateLimitResult.message,
      retryAfter: rateLimitResult.remainingTime
    });
  }

  try {
    let result = await SignIn(email, password);

    if (result.isSuccess) {
      // Reset rate limit on successful login
      resetRateLimit(clientIP);
      
      // Set access token (24 hours)
      setCookie('accessToken', result.accessToken, { 
        req, 
        res, 
        maxAge: 60 * 60 * 24,
        httpOnly: true,
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'strict'
      });

      // Set refresh token (7 days)
      setCookie('refreshToken', result.refreshToken, { 
        req, 
        res, 
        maxAge: 60 * 60 * 24 * 7,
        httpOnly: true,
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'strict'
      });

      res.status(200).json({ ok: true, error: "" });
      return;
    }

    res.status(400).json({ ok: false, error: result.message });
  } catch (error) {
    console.error('Sign-in error:', error);
    res.status(500).json({ ok: false, error: 'Internal server error' });
  }
}