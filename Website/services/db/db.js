import { MongoClient } from 'mongodb'

const url = process.env.mongo_url;
const options = {
    useUnifiedTopology: true,
    useNewUrlParser: true,
}

let client
let clientPromise


client = new MongoClient(url, options)
clientPromise = client.connect()

const mongoClient = await clientPromise;
export const db = client.db("LibriGenie");

export const users = db.collection('users');