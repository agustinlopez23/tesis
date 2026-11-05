"use client";

import Image from "next/image";
import { FormEvent, useCallback, useEffect, useRef, useState } from "react";
import { EmptyState, Icon } from "./ui";
import PhotoPicker from "./photo-picker";
import type { Photo } from "./business-profile";

type PhotosProps = { id: string; defaultOpen?: boolean; onCountChange?: (count: number) => void };
// `defaultOpen` opens and loads the manager when the dashboard reaches the Fotos section; `onCountChange` feeds the setup guide.
export default function BusinessPhotos({ id, defaultOpen = false, onCountChange }: PhotosProps) {
  const [open, setOpen] = useState(defaultOpen);
  const [photos, setPhotos] = useState<Photo[] | null>(null);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [pending, setPending] = useState(false);
  const [editing, setEditing] = useState<number | null>(null);
  const [removing, setRemoving] = useState<number | null>(null);
  const [file, setFile] = useState<File | null>(null);
  const [caption, setCaption] = useState("");
  const [revision, setRevision] = useState(0);
  const [preview, setPreview] = useState("");
  const previewUrls = useRef(new Set<string>());
  useEffect(() => {
    const urls = previewUrls.current;
    return () => urls.forEach((url) => URL.revokeObjectURL(url));
  }, []);
  const path = "/api/marketplace/businesses/" + id + "/photos";
  const freeSlot = [1, 2, 3, 4, 5].find((slot) => !photos?.some((photo) => photo.slot === slot));
  const slot = editing ?? freeSlot;

  const load = useCallback(async () => {
    setPending(true); setError("");
    try {
      const response = await fetch(path, { cache: "no-store", signal: AbortSignal.timeout(15000) });
      if (!response.ok) throw new Error(response.status === 401 ? "Tu sesión venció. Volvé a ingresar para administrar las fotos." : "No pudimos cargar las fotos.");
      setPhotos(await response.json());
    } catch (failure) { setError(failure instanceof Error && failure.message.startsWith("Tu sesión") ? failure.message : "No pudimos cargar las fotos."); }
    finally { setPending(false); }
  }, [path]);
  useEffect(() => {
    if (!defaultOpen) return;
    const timer = setTimeout(() => void load(), 0);
    return () => clearTimeout(timer);
  }, [defaultOpen, load]);
  useEffect(() => { if (photos) onCountChange?.(photos.length); }, [photos, onCountChange]);
  function resetFile() {
    previewUrls.current.forEach((url) => URL.revokeObjectURL(url));
    previewUrls.current.clear(); setPreview(""); setFile(null); setRevision((value) => value + 1);
  }
  function selectFile(selected: File | undefined) {
    previewUrls.current.forEach((url) => URL.revokeObjectURL(url));
    previewUrls.current.clear(); setPreview(""); setFile(null); setError(""); setMessage("");
    if (!selected) return;
    if (selected.size > 5 * 1024 * 1024) { setError("La foto supera los 5 MB. Elegí una versión más liviana."); return; }
    if (!["image/jpeg", "image/png", "image/webp"].includes(selected.type)) { setError("Elegí una foto JPEG, PNG o WebP."); return; }
    setFile(selected);
    if (typeof URL.createObjectURL === "function") {
      const url = URL.createObjectURL(selected); previewUrls.current.add(url); setPreview(url);
    }
  }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!file || !slot) return;
    setPending(true); setError(""); setMessage("");
    const form = new FormData(); form.append("file", file); form.append("caption", caption);
    try {
      const response = await fetch(path + "/" + slot, { method: "PUT", headers: { "X-Requested-With": "marketplace" },
        body: form, cache: "no-store", signal: AbortSignal.timeout(30000) });
      if (!response.ok) {
        setError(response.status === 401 ? "Tu sesión venció. Volvé a ingresar." : response.status === 413 ?
          "La foto supera el tamaño permitido de 5 MB." : response.status === 400 ?
            "No pudimos guardar la foto. Revisá que sea JPEG, PNG o WebP, de hasta 5 MB y 20 megapíxeles." : "No pudimos guardar la foto. Volvé a intentarlo.");
        return;
      }
      const saved: Photo = await response.json();
      setPhotos((current) => [...(current ?? []).filter((photo) => photo.slot !== saved.slot), saved].sort((a, b) => a.slot - b.slot));
      setEditing(null); setCaption(""); resetFile(); setMessage("Foto guardada.");
    } catch { setError("No pudimos guardar la foto. Revisá tu conexión y volvé a intentarlo."); }
    finally { setPending(false); }
  }
  async function remove(slot: number) {
    setPending(true); setError(""); setMessage("");
    try {
      const response = await fetch(path + "/" + slot, { method: "DELETE", headers: { "X-Requested-With": "marketplace" },
        cache: "no-store", signal: AbortSignal.timeout(15000) });
      if (!response.ok && response.status !== 404) { setError(response.status === 401 ? "Tu sesión venció. Volvé a ingresar." : "No pudimos eliminar la foto."); return; }
      setPhotos((current) => current?.filter((photo) => photo.slot !== slot) ?? []);
      setRemoving(null); setMessage("Foto eliminada.");
      if (editing === slot) { setEditing(null); resetFile(); setCaption(""); }
    } catch { setError("No pudimos eliminar la foto. Volvé a intentarlo."); }
    finally { setPending(false); }
  }

  return <section className="panel" aria-labelledby={"photos-title-" + id}>
    <div className="panel-heading"><div><p className="eyebrow">La identidad de tu negocio</p><h2 id={"photos-title-" + id}>Mostrá lo que te hace especial.</h2><p>Sumá hasta 5 fotos de tu local y tu propuesta.</p></div>
      <div className="panel-actions"><button type="button" className="button button-outline" aria-expanded={open} onClick={() => {
        setOpen(!open); if (!open && photos === null) void load();
      }}>{open ? "Cerrar fotos" : "Administrar fotos"}<Icon name={open ? "close" : "plus"} /></button></div></div>
    {open && <div className="photo-manager-content">
      {error && <p role="alert" className="error">{error}</p>}
      {message && <p role="status" className="photo-message"><Icon name="check" />{message}</p>}
      {photos === null ? pending ? <p role="status">Cargando fotos…</p> : <button className="button button-outline" onClick={load}>Reintentar fotos</button> : <>
        {photos.length === 0 ? <EmptyState compact icon="image" title="Todavía no agregaste fotos.">La primera foto que subas será la portada de tu perfil.</EmptyState> :<div className="owner-photo-grid">{photos.map((photo, index) => <article key={photo.slot}>
          <div className="owner-photo"><Image src={path + "/" + photo.slot + "?v=" + revision} alt={photo.caption || "Foto " + photo.slot} fill unoptimized /><span>{index === 0 ? "Portada" : "Foto " + photo.slot}</span></div>
          <p>{photo.caption || "Sin descripción"}</p><div className="photo-actions">
            <button type="button" disabled={pending} onClick={() => { setEditing(photo.slot); setCaption(photo.caption); resetFile(); setError(""); setMessage(""); }} aria-label={"Reemplazar foto " + photo.slot}>Reemplazar</button>
            <button type="button" disabled={pending} onClick={() => setRemoving(photo.slot)} aria-label={"Eliminar foto " + photo.slot}>Eliminar</button>
          </div>{removing === photo.slot && <div className="photo-confirm"><p>¿Eliminar esta foto?</p><button type="button" disabled={pending} onClick={() => remove(photo.slot)}>Confirmar eliminación</button><button type="button" disabled={pending} onClick={() => setRemoving(null)}>Cancelar eliminación</button></div>}
        </article>)}</div>}
        <p className="field-help">La primera foto disponible es la portada. Las fotos siguen el estado de publicación de tu negocio.</p>
        {slot ? <form className="photo-upload" onSubmit={save}><fieldset disabled={pending}><legend>{editing ? "Reemplazar foto " + editing : "Agregar una foto"}</legend>
          <PhotoPicker key={revision} label="Archivo de foto" fileName={file?.name} describedBy={"photo-help-" + id} onSelect={(selected) => selectFile(selected ?? undefined)} />
          <p id={"photo-help-" + id} className="field-help">JPEG, PNG o WebP · Hasta 5 MB y 20 megapíxeles. Usá fotos propias o que tengas permiso de publicar.</p>
          {file && preview && <div className="upload-preview"><Image src={preview} alt="Vista previa de la foto elegida" width={140} height={100} unoptimized /></div>}
          <label htmlFor={"photo-caption-" + id}>Descripción de la foto</label><input id={"photo-caption-" + id} maxLength={160} placeholder="Por ejemplo: el patio de nuestro café" value={caption} onChange={(event) => setCaption(event.target.value)} />
          <p className="field-help">Una descripción breve ayuda a entender la imagen.</p>
          <div className="photo-upload-actions"><button className="button button-primary" type="submit" disabled={pending || !file}>{pending ? "Guardando…" : "Guardar foto"}<Icon name="check" /></button>
            {editing && <button className="button button-outline" type="button" onClick={() => { setEditing(null); setCaption(""); resetFile(); }}>Cancelar reemplazo</button>}</div>
        </fieldset></form> : <p className="photos-empty">Ya tenés 5 fotos. Podés reemplazar o eliminar cualquiera.</p>}
      </>}
    </div>}
  </section>;
}
