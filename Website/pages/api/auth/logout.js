import { deleteCookie } from 'cookies-next';

export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ ok: false, error: 'Method not allowed' });
  }

  try {
    // Clear access token
    deleteCookie('accessToken', { req, res });
    
    // Clear refresh token
    deleteCookie('refreshToken', { req, res });

    res.status(200).json({ ok: true, message: 'Logged out successfully' });
  } catch (error) {
    console.error('Logout error:', error);
    res.status(500).json({ ok: false, error: 'Internal server error' });
  }
}
