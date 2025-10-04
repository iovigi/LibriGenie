import { IsAuth } from "../../../services/auth/is-auth-service"
import { SaveSettings, AddSetting, UpdateSetting, DeleteSetting } from "../../../services/dashboard/settings-service"

export default async function handler(req, res) {
    const currentUser = req.cookies['token'];

    if (!currentUser) {
        res.status(401).json({ ok: false });
    }

    if (!await IsAuth(currentUser)) {
        res.status(401).json({ ok: false });
    }

    const { action, settingId, setting } = req.body;

    try {
        switch (action) {
            case 'add':
                await AddSetting(currentUser, setting);
                break;
            case 'update':
                await UpdateSetting(currentUser, settingId, setting);
                break;
            case 'delete':
                await DeleteSetting(currentUser, settingId);
                break;
            default:
                // Legacy support - save all settings
                await SaveSettings(currentUser, req.body);
                break;
        }

        res.status(200).json({ ok: true });
    } catch (error) {
        res.status(500).json({ ok: false, error: error.message });
    }
}