import { users } from "../db/db";
import { generateAccessToken } from "./token-generation";
import bcrypt from 'bcrypt'

export async function SignIn(email, password) {
    let result = await users.find({ email: email }).limit(1).toArray();

    if (result.length == 0) {
        return { isSuccess: false, message: "Invalid email" };
    }

    let user = result[0];
    const isSuccess = await bcrypt.compare(password, user.password);

    if (isSuccess) {
        return { isSuccess: true, token: await generateAccessToken(user.id) };
    }

    return { isSuccess: false, message: "Invalid email or password" };
}

export async function SignUp(email, password) {
    let result = await users.find({ email: email }).limit(1).toArray();
    if (result.length > 0) {
        return { isSuccess: false, message: "User already exists" };
    }
    const hashedPassword = await bcrypt.hash(password, 10);
    let user = { id: generateUUID(), email: email, password: hashedPassword };
    await users.insertOne(user);

    return { isSuccess: true, token: await generateAccessToken(user.id) };
}

function generateUUID() { // Public Domain/MIT
    var d = new Date().getTime();//Timestamp
    var d2 = ((typeof performance !== 'undefined') && performance.now && (performance.now()*1000)) || 0;//Time in microseconds since page-load or 0 if unsupported
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        var r = Math.random() * 16;//random number between 0 and 16
        if(d > 0){//Use timestamp until depleted
            r = (d + r)%16 | 0;
            d = Math.floor(d/16);
        } else {//Use microseconds since page-load if supported
            r = (d2 + r)%16 | 0;
            d2 = Math.floor(d2/16);
        }
        return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
}


