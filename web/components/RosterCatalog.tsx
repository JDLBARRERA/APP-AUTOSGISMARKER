"use client";

import { useState } from "react";
import { Check, ChevronDown, Pencil, Plus, Trash2, X } from "lucide-react";

export interface TeamEntry {
  name: string;
  color: string;
}

interface RosterCatalogProps {
  teams: TeamEntry[];
  sponsors: string[];
  onAddTeam: (name: string) => void;
  onRenameTeam: (previous: string, next: string) => void;
  onRemoveTeam: (name: string) => void;
  onAddSponsor: (name: string) => void;
  onRenameSponsor: (previous: string, next: string) => void;
  onRemoveSponsor: (name: string) => void;
}

/**
 * Lista editable de escuderías y patrocinios de la parrilla.
 */
export function RosterCatalog({
  teams,
  sponsors,
  onAddTeam,
  onRenameTeam,
  onRemoveTeam,
  onAddSponsor,
  onRenameSponsor,
  onRemoveSponsor,
}: RosterCatalogProps) {
  return (
    <div className="grid gap-3">
      <NameList
        title="Escuderías"
        placeholder="Nueva escudería"
        addLabel="Agregar escudería"
        items={teams.map((team) => ({ id: team.name, label: team.name, color: team.color }))}
        onAdd={onAddTeam}
        onRename={onRenameTeam}
        onRemove={onRemoveTeam}
      />
      <NameList
        title="Patrocinios"
        placeholder="Nuevo patrocinio"
        addLabel="Agregar patrocinio"
        items={sponsors.map((sponsor) => ({ id: sponsor, label: sponsor }))}
        onAdd={onAddSponsor}
        onRename={onRenameSponsor}
        onRemove={onRemoveSponsor}
      />
    </div>
  );
}

function NameList({
  title,
  placeholder,
  addLabel,
  items,
  onAdd,
  onRename,
  onRemove,
}: {
  title: string;
  placeholder: string;
  addLabel: string;
  items: { id: string; label: string; color?: string }[];
  onAdd: (name: string) => void;
  onRename: (previous: string, next: string) => void;
  onRemove: (name: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState("");
  const [editing, setEditing] = useState<string | null>(null);
  const [editValue, setEditValue] = useState("");

  function submitNew() {
    const name = draft.trim();
    if (!name) return;
    onAdd(name);
    setDraft("");
  }

  function beginEdit(id: string) {
    setEditing(id);
    setEditValue(id);
  }

  function commitEdit() {
    if (!editing) return;
    const next = editValue.trim();
    if (next && next !== editing) onRename(editing, next);
    setEditing(null);
  }

  return (
    <div className="grid min-w-0 gap-2 rounded-2xl border border-white/10 bg-black/30 p-3">
      <button
        type="button"
        onClick={() => setOpen((current) => !current)}
        aria-expanded={open}
        className="flex w-full items-center justify-between gap-2 text-left"
      >
        <span className="text-[10px] font-semibold tracking-[0.16em] text-white/55">
          {title.toUpperCase()} · {items.length}
        </span>
        <ChevronDown className={`h-4 w-4 shrink-0 text-white/70 transition ${open ? "rotate-180" : ""}`} />
      </button>
      {open ? (
      <>
      <ul className="grid max-h-44 gap-1.5 overflow-y-auto">
        {items.map((item) => (
          <li key={item.id} className="flex min-h-10 min-w-0 items-center gap-2 rounded-xl bg-white/5 px-2.5">
            {item.color ? (
              <span className="h-2.5 w-2.5 shrink-0 rounded-full" style={{ backgroundColor: item.color }} />
            ) : null}
            {editing === item.id ? (
              <input
                value={editValue}
                onChange={(event) => setEditValue(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Enter") commitEdit();
                  if (event.key === "Escape") setEditing(null);
                }}
                aria-label={`Nuevo nombre de ${item.label}`}
                className="min-w-0 flex-1 rounded-lg border border-cyan-300/40 bg-black/50 px-2 py-1.5 text-sm outline-none"
                autoFocus
              />
            ) : (
              <span className="min-w-0 flex-1 truncate text-sm">{item.label}</span>
            )}
            <span className="flex shrink-0 items-center gap-1">
              {editing === item.id ? (
                <>
                  <button type="button" onClick={commitEdit} className="grid h-8 w-8 place-items-center text-lime-300" aria-label={`Guardar ${item.label}`}>
                    <Check className="h-4 w-4" />
                  </button>
                  <button type="button" onClick={() => setEditing(null)} className="grid h-8 w-8 place-items-center text-white/50" aria-label="Cancelar edición">
                    <X className="h-4 w-4" />
                  </button>
                </>
              ) : (
                <>
                  <button type="button" onClick={() => beginEdit(item.id)} className="grid h-8 w-8 place-items-center text-white/70" aria-label={`Editar ${item.label}`}>
                    <Pencil className="h-4 w-4" />
                  </button>
                  <button
                    type="button"
                    onClick={() => onRemove(item.id)}
                    disabled={items.length <= 1}
                    className="grid h-8 w-8 place-items-center text-white/55 disabled:opacity-25"
                    aria-label={`Quitar ${item.label}`}
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </>
              )}
            </span>
          </li>
        ))}
      </ul>
      <div className="flex min-w-0 items-center gap-2">
        <input
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter") submitNew();
          }}
          placeholder={placeholder}
          aria-label={placeholder}
          className="min-w-0 flex-1 rounded-xl border border-white/10 bg-black/40 px-3 py-2.5 text-sm outline-none placeholder:text-white/30"
        />
        <button
          type="button"
          onClick={submitNew}
          className="grid h-11 w-11 shrink-0 place-items-center rounded-xl border border-cyan-300/40 bg-cyan-400/15 text-cyan-100"
          aria-label={addLabel}
        >
          <Plus className="h-4 w-4" />
        </button>
      </div>
      </>
      ) : null}
    </div>
  );
}
