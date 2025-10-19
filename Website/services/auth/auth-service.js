import { users } from "../db/db";
import { generateAccessToken, generateRefreshToken } from "./token-generation";
import { validatePassword, validateEmail } from "./password-validation";
import bcrypt from 'bcrypt'

export async function SignIn(email, password) {
    // Validate email format
    const emailValidation = validateEmail(email);
    if (!emailValidation.isValid) {
        return { isSuccess: false, message: "Invalid email format" };
    }

    let result = await users.find({ email: email }).limit(1).toArray();

    if (result.length == 0) {
        return { isSuccess: false, message: "Invalid email or password" };
    }

    let user = result[0];
    const isSuccess = await bcrypt.compare(password, user.password);

    if (isSuccess) {
        // Update last login
        await users.updateOne(
            { id: user.id },
            { $set: { lastLogin: new Date() } }
        );

        // Prepare user claims for JWT
        const userClaims = {
            email: user.email,
            role: user.role || 'user',
            permissions: user.permissions || [],
            lastLogin: new Date().toISOString(),
            accountStatus: user.accountStatus || 'active'
        };

        const accessToken = await generateAccessToken(user.id, userClaims);
        const refreshToken = await generateRefreshToken(user.id, userClaims);

        return { 
            isSuccess: true, 
            accessToken: accessToken,
            refreshToken: refreshToken
        };
    }

    return { isSuccess: false, message: "Invalid email or password" };
}

export async function SignUp(email, password) {
    // Validate email
    const emailValidation = validateEmail(email);
    if (!emailValidation.isValid) {
        return { isSuccess: false, message: emailValidation.error };
    }

    // Validate password
    const passwordValidation = validatePassword(password);
    if (!passwordValidation.isValid) {
        return { isSuccess: false, message: passwordValidation.errors.join(", ") };
    }

    let result = await users.find({ email: email }).limit(1).toArray();
    if (result.length > 0) {
        return { isSuccess: false, message: "User already exists" };
    }
    
    const hashedPassword = await bcrypt.hash(password, 12); // Increased salt rounds
    let user = { 
        id: generateUUID(), 
        email: email, 
        password: hashedPassword,
        role: 'user',
        permissions: [],
        accountStatus: 'active',
        createdAt: new Date(),
        lastLogin: null
    };
    await users.insertOne(user);

    // Prepare user claims for JWT
    const userClaims = {
        email: user.email,
        role: user.role,
        permissions: user.permissions,
        lastLogin: new Date().toISOString(),
        accountStatus: user.accountStatus
    };

    const accessToken = await generateAccessToken(user.id, userClaims);
    const refreshToken = await generateRefreshToken(user.id, userClaims);

    return { 
        isSuccess: true, 
        accessToken: accessToken,
        refreshToken: refreshToken
    };
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


