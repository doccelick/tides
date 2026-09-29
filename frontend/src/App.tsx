import { ApiStatus } from "@/components/ApiStatus";

function App() {
  return (
    <main className="flex min-h-svh flex-col items-center justify-center gap-4">
      <h1 className="text-4xl font-semibold">Tides</h1>
      <ApiStatus />
    </main>
  );
}

export default App;
