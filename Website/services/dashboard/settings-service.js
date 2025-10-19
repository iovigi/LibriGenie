import { verifyAccessToken } from "../auth/token-generation";
import { users } from "../db/db";

export async function SaveSettings(token, settings) {
    const tokenResult = await verifyAccessToken(token);
    if (!tokenResult || tokenResult.error) {
        throw new Error('Invalid or expired token');
    }
    
    let userId = tokenResult.userId;
    let result = await users.find({ id: userId }).limit(1).toArray();
    let user = result[0];
    user.settings = settings;

    await users.updateOne({ id: userId }, { $set: user });
    let update_result = await fetch(process.env.store_settings_url, { method: 'POST', body: JSON.stringify({ userId: userId, settings: settings }), headers: { 'Content-Type': 'application/json', 'X-Key': process.env.store_settings_key } });
}

export async function GetSettings(token) {
    const tokenResult = await verifyAccessToken(token);
    if (!tokenResult || tokenResult.error) {
        throw new Error('Invalid or expired token');
    }
    
    let userId = tokenResult.userId;
    let result = await users.find({ id: userId }).limit(1).toArray();
    let user = result[0];
    return user.settings || [];
}

export async function AddSetting(token, setting) {
    const tokenResult = await verifyAccessToken(token);
    if (!tokenResult || tokenResult.error) {
        throw new Error('Invalid or expired token');
    }
    
    let userId = tokenResult.userId;
    
    // Generate a new ID for the setting
    const newSetting = {
        ...setting,
        id: require('crypto').randomBytes(12).toString('hex')
    };

    await users.updateOne(
        { id: userId },
        { $push: { settings: newSetting } }
    );
    
    // Update external service
    await fetch(process.env.store_settings_url, { 
        method: 'POST', 
        body: JSON.stringify({ userId: userId, action: 'add', setting: newSetting }), 
        headers: { 'Content-Type': 'application/json', 'X-Key': process.env.store_settings_key } 
    });
}

export async function UpdateSetting(token, settingId, setting) {
    const tokenResult = await verifyAccessToken(token);
    if (!tokenResult || tokenResult.error) {
        throw new Error('Invalid or expired token');
    }
    
    let userId = tokenResult.userId;

    await users.updateOne(
        { id: userId, "settings.id": settingId },
        { $set: { "settings.$": { ...setting, id: settingId } } }
    );
    
    // Update external service
    await fetch(process.env.store_settings_url, { 
        method: 'POST', 
        body: JSON.stringify({ userId: userId, action: 'update', settingId: settingId, setting: setting }), 
        headers: { 'Content-Type': 'application/json', 'X-Key': process.env.store_settings_key } 
    });
}

export async function DeleteSetting(token, settingId) {
    const tokenResult = await verifyAccessToken(token);
    if (!tokenResult || tokenResult.error) {
        throw new Error('Invalid or expired token');
    }
    
    let userId = tokenResult.userId;

    await users.updateOne(
        { id: userId },
        { $pull: { settings: { id: settingId } } }
    );
    
    // Update external service
    await fetch(process.env.store_settings_url, { 
        method: 'POST', 
        body: JSON.stringify({ userId: userId, action: 'delete', settingId: settingId }), 
        headers: { 'Content-Type': 'application/json', 'X-Key': process.env.store_settings_key } 
    });
}