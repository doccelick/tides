import { MapPage } from "@/map/MapPage";
import { ApiStatus } from "@/components/ApiStatus";
import { SignInButton } from "@/auth/SignInButton";

function App() {
  return (
    <main className="relative">
      <MapPage />
      <div className="absolute top-3 right-3 flex flex-col items-end gap-2">
        <ApiStatus />
        <SignInButton />
      </div>
    </main>
  );
}

export default App;
