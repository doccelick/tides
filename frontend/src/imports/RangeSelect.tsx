import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { importPresets, resolvePreset, type UtcRange } from "./importPresets";

interface RangeSelectProps {
  now: Date;
  value: string;
  onValueChange: (label: string) => void;
  // Presets whose range this returns true for are marked as imported and cannot be picked.
  isImported?: (range: UtcRange) => boolean;
}

const groups = [...new Set(importPresets.map((preset) => preset.group))];

export function RangeSelect({
  now,
  value,
  onValueChange,
  isImported = () => false,
}: RangeSelectProps) {
  return (
    <Select
      value={value}
      onValueChange={(label) => {
        if (label !== null) onValueChange(label);
      }}
    >
      <SelectTrigger aria-label="Range" className="h-11 w-full">
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        {groups.map((group) => (
          <SelectGroup key={group}>
            <SelectLabel>{group}</SelectLabel>
            {importPresets
              .filter((preset) => preset.group === group)
              .map((preset) => {
                const range = resolvePreset(preset, now);
                if (range === null) return null;

                const imported = isImported(range);
                return (
                  <SelectItem key={preset.label} value={preset.label} disabled={imported}>
                    {imported ? `${preset.label} (imported)` : preset.label}
                  </SelectItem>
                );
              })}
          </SelectGroup>
        ))}
      </SelectContent>
    </Select>
  );
}
