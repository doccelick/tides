import { InteractionStatus } from "@azure/msal-browser";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { Button } from "@/components/ui/button";
import { apiScope } from "./authConfig";

export function SignInButton() {
  const { instance, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const isBusy = inProgress !== InteractionStatus.None;

  if (isAuthenticated) {
    return (
      <Button
        className="h-11"
        variant="outline"
        disabled={isBusy}
        onClick={() => void instance.logoutRedirect()}
      >
        Sign out
      </Button>
    );
  }

  // Asking for the API scope at sign-in means the first write needs no second redirect.
  return (
    <Button
      className="h-11"
      variant="outline"
      disabled={isBusy}
      onClick={() => void instance.loginRedirect({ scopes: [apiScope] })}
    >
      Sign in
    </Button>
  );
}
