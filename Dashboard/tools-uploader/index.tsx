import { registerHordePlugin, MountType } from "../../../plugins";
import { ToolsUploaderView } from "./toolsUploaderView";

registerHordePlugin({
   id: "toolsuploader",
   routes: [{ path: "upload-tool", element: <ToolsUploaderView /> }],
   mount: {
      type: MountType.TopNav,
      context: "Tools",
      text: "Manual Upload",
      route: "/upload-tool"
   },
});
