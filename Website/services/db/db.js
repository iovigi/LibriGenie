import { MongoClient } from 'mongodb'

const url = process.env.mongo_url;

let client
let clientPromise


client = new MongoClient(url)
clientPromise = client.connect()

const mongoClient = await clientPromise;
export const db = client.db("LibriGenie");

export const users = db.collection('users');