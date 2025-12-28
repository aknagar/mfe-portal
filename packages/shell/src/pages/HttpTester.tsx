import * as React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import { Input } from '../components/ui/input';
import { Label } from '../components/ui/label';
import { Textarea } from '../components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';

interface Header {
  key: string;
  value: string;
}

export const HttpTester: React.FC = () => {
  const [method, setMethod] = React.useState('GET');
  const [url, setUrl] = React.useState('');
  const [headers, setHeaders] = React.useState<Header[]>([{ key: '', value: '' }]);
  const [body, setBody] = React.useState('');
  const [response, setResponse] = React.useState<any>(null);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  const addHeader = () => {
    setHeaders([...headers, { key: '', value: '' }]);
  };

  const removeHeader = (index: number) => {
    setHeaders(headers.filter((_, i) => i !== index));
  };

  const updateHeader = (index: number, field: 'key' | 'value', value: string) => {
    const newHeaders = [...headers];
    newHeaders[index][field] = value;
    setHeaders(newHeaders);
  };

  const sendRequest = async () => {
    setLoading(true);
    setError(null);
    setResponse(null);

    try {
      const requestHeaders: Record<string, string> = {};
      headers.forEach((header) => {
        if (header.key && header.value) {
          requestHeaders[header.key] = header.value;
        }
      });

      const options: RequestInit = {
        method,
        headers: requestHeaders,
      };

      if (method !== 'GET' && method !== 'HEAD' && body) {
        options.body = body;
      }

      const startTime = performance.now();
      const res = await fetch(url, options);
      const endTime = performance.now();

      const contentType = res.headers.get('content-type');
      let responseData: any;

      if (contentType?.includes('application/json')) {
        responseData = await res.json();
      } else {
        responseData = await res.text();
      }

      const responseHeaders: Record<string, string> = {};
      res.headers.forEach((value, key) => {
        responseHeaders[key] = value;
      });

      setResponse({
        status: res.status,
        statusText: res.statusText,
        headers: responseHeaders,
        data: responseData,
        time: Math.round(endTime - startTime),
      });
    } catch (err: any) {
      setError(err.message || 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">HTTP Request Tester</h1>
        <p className="text-muted-foreground mt-2">
          Test HTTP endpoints and view responses
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Request</CardTitle>
          <CardDescription>Configure and send HTTP requests</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Method and URL */}
          <div className="flex gap-2">
            <div className="w-32">
              <Select value={method} onValueChange={setMethod}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="GET">GET</SelectItem>
                  <SelectItem value="POST">POST</SelectItem>
                  <SelectItem value="PUT">PUT</SelectItem>
                  <SelectItem value="PATCH">PATCH</SelectItem>
                  <SelectItem value="DELETE">DELETE</SelectItem>
                  <SelectItem value="HEAD">HEAD</SelectItem>
                  <SelectItem value="OPTIONS">OPTIONS</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="flex-1">
              <Input
                placeholder="https://api.example.com/endpoint"
                value={url}
                onChange={(e) => setUrl(e.target.value)}
              />
            </div>
            <Button onClick={sendRequest} disabled={loading || !url}>
              {loading ? 'Sending...' : 'Send'}
            </Button>
          </div>

          <Tabs defaultValue="headers">
            <TabsList>
              <TabsTrigger value="headers">Headers</TabsTrigger>
              <TabsTrigger value="body">Body</TabsTrigger>
            </TabsList>

            <TabsContent value="headers" className="space-y-2">
              {headers.map((header, index) => (
                <div key={index} className="flex gap-2">
                  <Input
                    placeholder="Header Key"
                    value={header.key}
                    onChange={(e) => updateHeader(index, 'key', e.target.value)}
                  />
                  <Input
                    placeholder="Header Value"
                    value={header.value}
                    onChange={(e) => updateHeader(index, 'value', e.target.value)}
                  />
                  <Button
                    variant="outline"
                    size="icon"
                    onClick={() => removeHeader(index)}
                    disabled={headers.length === 1}
                  >
                    ×
                  </Button>
                </div>
              ))}
              <Button variant="outline" onClick={addHeader}>
                Add Header
              </Button>
            </TabsContent>

            <TabsContent value="body">
              <Textarea
                placeholder="Request body (JSON, XML, etc.)"
                value={body}
                onChange={(e) => setBody(e.target.value)}
                className="min-h-[200px] font-mono text-sm"
                disabled={method === 'GET' || method === 'HEAD'}
              />
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>

      {/* Response */}
      {(response || error) && (
        <Card>
          <CardHeader>
            <CardTitle>Response</CardTitle>
            {response && (
              <CardDescription>
                Status: {response.status} {response.statusText} • Time: {response.time}ms
              </CardDescription>
            )}
          </CardHeader>
          <CardContent>
            {error ? (
              <div className="p-4 bg-destructive/10 text-destructive rounded-md">
                <p className="font-semibold">Error</p>
                <p>{error}</p>
              </div>
            ) : (
              <Tabs defaultValue="body">
                <TabsList>
                  <TabsTrigger value="body">Body</TabsTrigger>
                  <TabsTrigger value="headers">Headers</TabsTrigger>
                </TabsList>

                <TabsContent value="body">
                  <div className="bg-muted p-4 rounded-md">
                    <pre className="text-sm overflow-auto max-h-[400px]">
                      {typeof response.data === 'string'
                        ? response.data
                        : JSON.stringify(response.data, null, 2)}
                    </pre>
                  </div>
                </TabsContent>

                <TabsContent value="headers">
                  <div className="bg-muted p-4 rounded-md">
                    <pre className="text-sm overflow-auto max-h-[400px]">
                      {JSON.stringify(response.headers, null, 2)}
                    </pre>
                  </div>
                </TabsContent>
              </Tabs>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
};
