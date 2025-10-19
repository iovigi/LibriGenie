import { verifyRefreshToken, generateAccessToken } from '../../../services/auth/token-generation';
import { setCookie } from 'cookies-next';

export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ ok: false, error: 'Method not allowed' });
  }

  try {
    const refreshToken = req.cookies.refreshToken;

    if (!refreshToken) {
      return res.status(401).json({ ok: false, error: 'No refresh token provided' });
    }

    const result = await verifyRefreshToken(refreshToken);

    if (result && result.userId) {
      // Generate new access token with the same claims from refresh token
      const newAccessToken = await generateAccessToken(result.userId, result.claims);

      // Set new access token
      setCookie('accessToken', newAccessToken, { 
        req, 
        res, 
        maxAge: 60 * 60 * 24,
        httpOnly: true,
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'strict'
      });

      res.status(200).json({ ok: true, accessToken: newAccessToken });
    } else {
      res.status(401).json({ ok: false, error: 'Invalid or expired refresh token' });
    }
  } catch (error) {
    console.error('Token refresh error:', error);
    res.status(500).json({ ok: false, error: 'Internal server error' });
  }
}
