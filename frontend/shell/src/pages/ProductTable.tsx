import React, { useEffect, useState } from 'react';
import { config } from '../config';

interface Product {
  id: number;
  name: string;
  price: number;
  category?: string;
  description?: string;
}

export const ProductTable: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const apiBaseUrl = config.api.baseUrl;

  useEffect(() => {
    const fetchProducts = async () => {
      try {
        setLoading(true);
        const response = await fetch(`${apiBaseUrl}/api/Product`);
        
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        setProducts(data);
        setError(null);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to fetch products');
        console.error('Error fetching products:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchProducts();
  }, []);

  if (loading) {
    return (
      <div className="p-6">
        <div className="flex items-center justify-center">
          <div className="text-lg">Loading products...</div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-6">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <h2 className="text-lg font-semibold text-red-800 mb-2">Error</h2>
          <p className="text-red-600">{error}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6">
      <h1 className="text-3xl font-bold mb-6">Product List</h1>
      
      {products.length === 0 ? (
        <p className="text-muted-foreground">No products found.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-full bg-white border border-gray-200 rounded-lg shadow-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider border-b">
                  ID
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider border-b">
                  Name
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider border-b">
                  Price
                </th>
                {products.some(p => p.category) && (
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider border-b">
                    Category
                  </th>
                )}
                {products.some(p => p.description) && (
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider border-b">
                    Description
                  </th>
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {products.map((product) => (
                <tr key={product.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {product.id}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {product.name}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    ${product.price.toFixed(2)}
                  </td>
                  {products.some(p => p.category) && (
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                      {product.category || '-'}
                    </td>
                  )}
                  {products.some(p => p.description) && (
                    <td className="px-6 py-4 text-sm text-gray-600">
                      {product.description || '-'}
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      
      <div className="mt-4 text-sm text-muted-foreground">
        Total products: {products.length}
      </div>
    </div>
  );
};
