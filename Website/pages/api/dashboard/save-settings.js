import { IsAuth } from "../../../services/auth/is-auth-service"
import { SaveSettings } from "../../../services/dashboard/settings-service"

export default async function handler(req, res) {
    const currentUser = req.cookies['token'];

    if (!currentUser) {
        res.status(401).json({ ok: false });
    }

    if (!await IsAuth(currentUser)) {
        res.status(401).json({ ok: false });
    }

    SaveSettings(currentUser, req.body);

    res.status(200).json({ ok: true });
}