import { MongoClient } from 'mongodb'

const uri = 'mongodb://localhost:27017/'
const options = {
    useUnifiedTopology: true,
    useNewUrlParser: true,
}

let client
let clientPromise


client = new MongoClient(uri, options)
clientPromise = client.connect()

const mongoClient = await clientPromise;
export const db = client.db("LibriGenie");

export const users = db.collection('users');