import { IsAuth } from "../../../services/auth/is-auth-service";

export default async function handler(req, res) {
    if (req.method !== 'GET') {
        return res.status(405).json({ error: 'Method not allowed' });
    }

    try {
        // Check authentication
        const currentUser = req.cookies['token'];
        
        if (!currentUser) {
            return res.status(401).json({ error: 'No token provided' });
        }

        if (!await IsAuth(currentUser)) {
            return res.status(401).json({ error: 'Unauthorized' });
        }

        // Fetch products from Coinbase API
        const response = await fetch('https://api.exchange.coinbase.com/products');
        
        if (!response.ok) {
            throw new Error(`Coinbase API error: ${response.status}`);
        }

        const products = await response.json();

        // Filter for enabled products with USD or EUR pairs
        const enabledSymbols = products
            .filter(product => 
                product.status === 'online' && 
                (product.quote_currency === 'USD' || product.quote_currency === 'EUR')
            )
            .map(product => ({
                id: product.id,
                base_currency: product.base_currency,
                quote_currency: product.quote_currency,
                display_name: product.display_name,
                status: product.status,
                trading_enabled: product.trading_enabled
            }))
            .sort((a, b) => a.base_currency.localeCompare(b.base_currency));

        return res.status(200).json({
            success: true,
            symbols: enabledSymbols,
            total: enabledSymbols.length
        });

    } catch (error) {
        console.error('Error fetching crypto symbols:', error);
        return res.status(500).json({ 
            error: 'Failed to fetch crypto symbols',
            message: error.message 
        });
    }
} 