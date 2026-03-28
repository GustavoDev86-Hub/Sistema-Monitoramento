'use client'; 
import { useEffect, useState } from 'react';
import axios from 'axios'; 
import { Globe, CheckCircle, XCircle, RefreshCw } from 'lucide-react';

// Aqui a gente diz onde sua API .NET está rodando
const api = axios.create({
  baseURL: 'http://localhost:5075/api', 
});

export default function Dashboard() {
  const [ativos, setAtivos] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  // Função que vai lá no seu C# buscar os sites
  const carregarDados = async () => {
    try {
      const response = await api.get('/monitoramento/status');
      setAtivos(response.data);
    } catch (error) {
      console.error("Erro ao buscar API:", error);
    } finally {
      setLoading(false);
    }
  };

  // Isso aqui roda assim que a página abre
  useEffect(() => {
    carregarDados();
    const interval = setInterval(carregarDados, 15000); // Atualiza a cada 15s
    return () => clearInterval(interval);
  }, []);

  return (
    <main className="min-h-screen bg-black text-white p-10 font-sans">
      <div className="max-w-4xl mx-auto">
        <h1 className="text-3xl font-bold mb-8 flex items-center gap-3">
          <Globe className="text-blue-500" /> Painel de Monitoramento
        </h1>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {ativos.length > 0 ? (
            ativos.map((site: any) => (
              <div key={site.id} className="p-6 bg-gray-900 border border-gray-800 rounded-2xl shadow-xl">
                <div className="flex justify-between items-center mb-4">
                  <span className="text-gray-400 font-mono text-sm">{site.url}</span>
                  {site.estaOnline ? (
                    <CheckCircle className="text-green-500" />
                  ) : (
                    <XCircle className="text-red-500" />
                  )}
                </div>
                <p className="text-2xl font-bold">
                  {site.estaOnline ? 'SISTEMA UP' : 'SISTEMA DOWN'}
                </p>
                <p className="text-gray-500 text-xs mt-4 italic">
                  Viana SaaS - Verificado às {new Date(site.ultimaVerificacao).toLocaleTimeString()}
                </p>
              </div>
            ))
          ) : (
            <p className="text-gray-500">Buscando dados no servidor .NET...</p>
          )}
        </div>
      </div>
    </main>
  );
}
