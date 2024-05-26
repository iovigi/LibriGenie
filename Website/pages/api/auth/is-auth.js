import { IsAuth } from '../../../services/auth/auth-service'
import { setCookie, deleteCookie, hasCookie, getCookie, getCookies } from 'cookies-next';


export default async function handler(req, res) {
    let cookies = req.cookies;
    console.log({ cookies });
    console.log("Is Auth");
    if (await IsAuth(req, res)) {
        res.status(200).json({ ok: true, error: "" });
    }

    res.status(401).json({ ok: false });
}