import { IsAuth } from "../../../services/auth/is-auth-service"
import { SaveSettings, AddSetting, UpdateSetting, DeleteSetting } from "../../../services/dashboard/settings-service"

export default async function handler(req, res) {
    try {
        const accessToken = req.cookies['accessToken'];

        if (!accessToken) {
            return res.status(401).json({ ok: false, error: 'No access token found' });
        }

        if (!await IsAuth(accessToken)) {
            return res.status(401).json({ ok: false, error: 'Invalid or expired token' });
        }

        const { action, settingId, setting } = req.body;

        switch (action) {
            case 'add':
                await AddSetting(accessToken, setting);
                break;
            case 'update':
                await UpdateSetting(accessToken, settingId, setting);
                break;
            case 'delete':
                await DeleteSetting(accessToken, settingId);
                break;
            default:
                // Legacy support - save all settings
                await SaveSettings(accessToken, req.body);
                break;
        }

        res.status(200).json({ ok: true });
    } catch (error) {
        console.error('Save settings error:', error);
        res.status(500).json({ ok: false, error: error.message });
    }
}