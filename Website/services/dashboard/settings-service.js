import { verifyAccessToken } from "../auth/token-generation";
import { users } from "../db/db";

export async function SaveSettings(token, settings) {
    let userId = (await verifyAccessToken(token)).userId;

    let result = await users.find({ id: userId }).limit(1).toArray();
    let user = result[0];
    user.settings = settings;

    await users.updateOne({ id: userId }, { $set: user });
    let update_result = await fetch(process.env.store_settings_url, { method: 'POST', body: JSON.stringify({ userId: userId, settings: settings }), headers: { 'Content-Type': 'application/json', 'X-Key': process.env.store_settings_key } });
}

export async function GetSettings(token) {
    let userId = (await verifyAccessToken(token)).userId;
    let result = await users.find({ id: userId }).limit(1).toArray();
    let user = result[0];
    return user.settings;
}