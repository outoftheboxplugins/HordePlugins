import { Checkbox, Dropdown, IDropdownOption, MessageBar, MessageBarType, PrimaryButton, ProgressIndicator, Spinner, Stack, Text, TextField } from "@fluentui/react";
import { useEffect, useRef, useState } from "react";
import backend from "../../../src/backend";
import { GetToolSummaryResponse } from "../../../src/backend/Api";
import { useWindowSize } from "../../../src/base/utilities/hooks";
import { formatBytes } from "../../../src/base/utilities/stringUtills";
import { Breadcrumbs } from "../../../src/components/Breadcrumbs";
import { TopNav } from "../../../src/components/TopNav";
import { getHordeStyling } from "../../../src/styles/Styles";

type ToolsState =
   | { kind: "loading" }
   | { kind: "error"; message: string }
   | { kind: "ready"; items: GetToolSummaryResponse[] };

type UploadState =
   | { kind: "idle" }
   | { kind: "uploading"; progress: number }
   | { kind: "success"; deploymentId: string }
   | { kind: "error"; message: string };

// XHR instead of fetch so we can stream upload progress for large tool bundles.
// withCredentials piggybacks cookie auth, matching backend.fetch's default.
function uploadDeployment(toolId: string, file: File, version: string, createPaused: boolean, onProgress: (fraction: number) => void): Promise<string> {
   return new Promise((resolve, reject) => {
      const form = new FormData();
      form.append("version", version);
      form.append("createPaused", createPaused ? "true" : "false");
      form.append("file", file, file.name);

      const xhr = new XMLHttpRequest();
      xhr.open("POST", `/api/v1/tools/${encodeURIComponent(toolId)}/deployments`);
      xhr.withCredentials = true;
      xhr.upload.onprogress = e => { if (e.lengthComputable) onProgress(e.loaded / e.total); };
      xhr.onload = () => {
         let body: any = null;
         try { body = JSON.parse(xhr.responseText); } catch { /* use null */ }
         if (xhr.status >= 200 && xhr.status < 300) {
            resolve(body?.id ?? body?.deploymentId ?? "");
         } else {
            reject(new Error(body?.message ?? xhr.statusText ?? `HTTP ${xhr.status}`));
         }
      };
      xhr.onerror = () => reject(new Error("Network error"));
      xhr.onabort = () => reject(new Error("Upload aborted"));
      xhr.send(form);
   });
}

export const ToolsUploaderView: React.FC = () => {
   // state of all the uploadable tools available
   const [toolsState, setToolsState] = useState<ToolsState>({ kind: "loading" });

   // configurable deployment settings
   const [selectedToolId, setSelectedToolId] = useState<string>();
   const [file, setFile] = useState<File>();
   const [version, setVersion] = useState("1.0.0");
   const [createPaused, setCreatePaused] = useState(false);

   // upload lifecycle and drop zone interaction
   const [dragOver, setDragOver] = useState(false);
   const [uploadState, setUploadState] = useState<UploadState>({ kind: "idle" });
   const fileInputRef = useRef<HTMLInputElement>(null);

   useEffect(() => {
      let cancelled = false;
      backend.fetch.get("api/v1/tools-uploader/uploadable-tools")
         .then(r => { if (!cancelled) setToolsState({ kind: "ready", items: r.data as GetToolSummaryResponse[] }); })
         .catch(reason => { if (!cancelled) setToolsState({ kind: "error", message: `${reason}` }); });
      return () => { cancelled = true; };
   }, []);

   const onDismissUploadState = () => {
      setUploadState({ kind: "idle" });
   };

   const onToolSelect = (_e: React.FormEvent<HTMLDivElement>, opt?: IDropdownOption) => {
      const id = opt?.key as string;
      setSelectedToolId(id);
      const tool = tools.find(t => t.id === id);
      setVersion(tool?.version ?? "1.0.0");
   };

   const onVersionChange = (_e: React.FormEvent<HTMLInputElement | HTMLTextAreaElement>, v?: string) => {
      setVersion(v ?? "");
   };

   const onCreatePausedChange = (_e?: React.FormEvent<HTMLElement | HTMLInputElement>, c?: boolean) => {
      setCreatePaused(!!c);
   };

   const onDragOver = (e: React.DragEvent<HTMLDivElement>) => {
      e.preventDefault();
      if (!uploading) setDragOver(true);
   };

   const onDragLeave = () => {
      setDragOver(false);
   };

   const onDropZoneClick = () => {
      if (!uploading) fileInputRef.current?.click();
   };

   const onDrop = (e: React.DragEvent<HTMLDivElement>) => {
      e.preventDefault();
      setDragOver(false);
      if (!uploading) setFile(e.dataTransfer.files?.[0]);
   };

   const onFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      setFile(e.target.files?.[0]);
   };

   const submit = async () => {
      if (!canSubmit) return;
      setUploadState({ kind: "uploading", progress: 0 });
      try {
         const deploymentId = await uploadDeployment(selectedToolId, file, version, createPaused, p => setUploadState({ kind: "uploading", progress: p }));
         setUploadState({ kind: "success", deploymentId });
      } catch (e: any) {
         setUploadState({ kind: "error", message: e?.message ?? "Upload failed" });
      }
   };

   const uploading = uploadState.kind === "uploading";
   const tools = toolsState.kind === "ready" ? toolsState.items : [];
   const canSubmit = !!selectedToolId && !!file && !!version && !uploading;

   const toolOptions: IDropdownOption[] = tools.map(t => ({
      key: t.id,
      text: t.name + (t.category ? `  ·  ${t.category}` : ""),
   }));

   const windowSize = useWindowSize();
   const { hordeClasses, modeColors } = getHordeStyling();
   const vw = Math.max(document.documentElement.clientWidth, window.innerWidth || 0);
   const centerAlign = vw / 2 - 720;
   const key = `windowsize_tools_uploader_${windowSize.width}_${windowSize.height}`;

   return (
      <Stack className={hordeClasses.horde}>
         <TopNav />
         <Breadcrumbs items={[{ text: "Upload Tool" }]} />
         <Stack styles={{ root: { width: "100%", backgroundColor: modeColors.background } }}>
            <Stack style={{ width: "100%", backgroundColor: modeColors.background }}>
               <Stack style={{ position: "relative", width: "100%", height: "calc(100vh - 148px)" }}>
                  <div style={{ overflowX: "auto", overflowY: "visible" }}>
                     <Stack horizontal style={{ paddingTop: 12, paddingBottom: 48 }}>
                        <Stack key={key} style={{ paddingLeft: centerAlign }} />
                        <Stack style={{ width: 1440 }} tokens={{ childrenGap: 12 }}>

                           {uploadState.kind === "success" && (
                              <MessageBar messageBarType={MessageBarType.success} onDismiss={onDismissUploadState}>
                                 {uploadState.deploymentId ? `Deployment ${uploadState.deploymentId} uploaded.` : "Uploaded."}{" "}
                                 {createPaused ? "It was created paused — activate it from the Tools page." : "It is rolling out now."}
                              </MessageBar>
                           )}
                           {uploadState.kind === "error" && (
                              <MessageBar messageBarType={MessageBarType.error} onDismiss={onDismissUploadState}>
                                 Upload failed: {uploadState.message}
                              </MessageBar>
                           )}
                           {toolsState.kind === "error" && (
                              <MessageBar messageBarType={MessageBarType.error}>
                                 Failed to load tools: {toolsState.message}
                              </MessageBar>
                           )}

                           <Stack className={hordeClasses.raised} tokens={{ childrenGap: 12 }} style={{ padding: 16 }}>
                              <Text variant="mediumPlus">Upload Tool Deployment</Text>

                              <Stack horizontal tokens={{ childrenGap: 12 }} verticalAlign="end">
                                 {toolsState.kind === "loading" && <Spinner />}
                                 {toolsState.kind === "ready" && tools.length === 0 && (
                                    <Text>No tools available. Configure tools in globals.json first.</Text>
                                 )}
                                 {toolsState.kind === "ready" && tools.length > 0 && (
                                    <Dropdown
                                       label="Tool"
                                       placeholder="Select a tool"
                                       options={toolOptions}
                                       selectedKey={selectedToolId ?? null}
                                       disabled={uploading}
                                       styles={{ root: { minWidth: 220 } }}
                                       onChange={onToolSelect} />
                                 )}
                                 <TextField
                                    label="Version"
                                    required
                                    value={version}
                                    disabled={uploading}
                                    styles={{ root: { width: 140 } }}
                                    onChange={onVersionChange} />
                                 <Stack style={{ paddingBottom: 4 }}>
                                    <Checkbox
                                       label="Create paused"
                                       checked={createPaused}
                                       disabled={uploading}
                                       onChange={onCreatePausedChange} />
                                 </Stack>
                                 <Stack style={{ paddingBottom: 4 }}>
                                    <PrimaryButton text={uploading ? "Uploading…" : "Upload"} disabled={!canSubmit} onClick={submit} />
                                 </Stack>
                              </Stack>

                              <div
                                 onDragOver={onDragOver}
                                 onDragLeave={onDragLeave}
                                 onDrop={onDrop}
                                 onClick={onDropZoneClick}
                                 style={{
                                    border: `2px dashed ${dragOver ? "#0078d4" : "#888"}`,
                                    borderRadius: 6,
                                    padding: 20,
                                    textAlign: "center",
                                    cursor: uploading ? "not-allowed" : "pointer",
                                    opacity: uploading ? 0.6 : 1,
                                 }}>
                                 {file
                                    ? <Text>{file.name} — {formatBytes(file.size)}</Text>
                                    : <Text>Drop a .zip here, or click to browse.</Text>}
                              </div>
                              <input ref={fileInputRef} type="file" accept=".zip" style={{ display: "none" }} onChange={onFileInputChange} />

                              {uploading && (
                                 <ProgressIndicator
                                    label={uploadState.progress >= 1 ? "Finalizing on server…" : "Uploading…"}
                                    description={`${Math.round(uploadState.progress * 100)}%`}
                                    percentComplete={uploadState.progress} />
                              )}
                           </Stack>

                        </Stack>
                     </Stack>
                  </div>
               </Stack>
            </Stack>
         </Stack>
      </Stack>
   );
};
