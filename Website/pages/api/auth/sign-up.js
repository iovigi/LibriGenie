import { SignUp } from '../../../services/auth/auth-service'
import { setCookie, deleteCookie, hasCookie, getCookie, getCookies } from 'cookies-next';

export default async function handler(req, res) {
  const { email, password } = req.body
  let result = await SignUp(email, password);

  if (result.isSuccess) {
    setCookie('token', result.token, { req, res, maxAge: 60 * 60 * 24 * 7 })
    res.status(200).json({ ok: true, error: "" });

    return;
  }

  res.status(400).json({ ok: false, error: result.message })
}