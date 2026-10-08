import { MapPage } from "@/map/MapPage";
import { ApiStatus } from "@/components/ApiStatus";

function App() {
  return (
    <main className="relative">
      <MapPage />
      <div className="absolute top-3 right-3">
        <ApiStatus />
      </div>
    </main>
  );
}

export default App;
