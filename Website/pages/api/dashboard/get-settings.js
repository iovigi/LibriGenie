import { IsAuth } from "../../../services/auth/is-auth-service"
import { GetSettings } from "../../../services/dashboard/settings-service"

export default async function handler(req, res) {
    const currentUser = req.cookies['token'];

    if (!currentUser) {
        res.status(401).json({ ok: false });
    }

    if (!await IsAuth(currentUser)) {
        res.status(401).json({ ok: false });
    }

    let settings = await GetSettings(currentUser);

    res.status(200).json({ ok: true, settings : settings });
}