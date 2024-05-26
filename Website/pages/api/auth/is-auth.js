import { IsAuth } from "../../../services/auth/is-auth-service"

export default async function handler(req, res) {
    const currentUser = req.cookies.get('token')

    if(!currentUser){
        res.status(401).json({ ok: false });
    }

    if (await IsAuth(currentUser.value)) {
        res.status(200).json({ ok: true, error: "" });
    }

    res.status(401).json({ ok: false });
}