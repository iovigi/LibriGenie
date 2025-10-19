import { IsAuth } from "../../../services/auth/is-auth-service"
import { GetSettings } from "../../../services/dashboard/settings-service"

export default async function handler(req, res) {
    try {
        const accessToken = req.cookies['accessToken'];

        if (!accessToken) {
            return res.status(401).json({ ok: false, error: 'No access token found' });
        }

        if (!await IsAuth(accessToken)) {
            return res.status(401).json({ ok: false, error: 'Invalid or expired token' });
        }

        let settings = await GetSettings(accessToken);

        res.status(200).json({ ok: true, settings: settings });
    } catch (error) {
        console.error('Get settings error:', error);
        res.status(500).json({ ok: false, error: 'Internal server error' });
    }
}