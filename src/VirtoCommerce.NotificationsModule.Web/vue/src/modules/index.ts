import * as blades from "./pages";
import * as locales from "./locales";
import { defineAppModule } from "@vc-shell/framework";
import "./styles.css";

// No external widgets: the store blade's notification widgets belong to the Store module and the order
// blade's notification log to the Orders module, which port them themselves.

export default defineAppModule({
  blades,
  locales,
});

export * from "./pages";
