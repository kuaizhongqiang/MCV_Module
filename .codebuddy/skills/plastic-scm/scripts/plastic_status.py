# Plastic SCM Status Checker

import subprocess
import sys
import os

def run_cm(args):
    """Run a cm command and return output."""
    try:
        result = subprocess.run(['cm'] + args, capture_output=True, text=True, encoding='utf-8', timeout=15)
        return result.stdout.strip(), result.stderr.strip(), result.returncode
    except FileNotFoundError:
        print("[ERROR] cm CLI not found. Is Plastic SCM installed and in PATH?")
        sys.exit(1)
    except subprocess.TimeoutExpired:
        print("[ERROR] cm command timed out.")
        sys.exit(1)

def main():
    print("=" * 50)
    print("  Plastic SCM Workspace Status")
    print("=" * 50)

    # Current branch
    branch_out, _, _ = run_cm(['status', '--head'])
    for line in branch_out.split('\n'):
        if 'Branch:' in line or 'br:' in line.lower():
            print(f"  Branch  : {line.split(':', 1)[-1].strip()}")
            break
    else:
        # Alternative: use 'cm showselector'
        sel, _, _ = run_cm(['showselector', '.'])
        if sel:
            print(f"  Selector: {sel}")

    # Pending changes summary
    pending, _, _ = run_cm(['status', '--short'])
    pending_lines = [l for l in pending.split('\n') if l.strip() and not l.startswith('#')]
    if pending_lines:
        added = sum(1 for l in pending_lines if l.strip().startswith('A'))
        changed = sum(1 for l in pending_lines if l.strip().startswith('C'))
        deleted = sum(1 for l in pending_lines if l.strip().startswith('D'))
        moved = sum(1 for l in pending_lines if l.strip().startswith('M'))
        private = sum(1 for l in pending_lines if l.strip().startswith('P'))
        total = len(pending_lines)
        print(f"  Pending : {total} files")
        if added: print(f"    Added    : {added}")
        if changed: print(f"    Changed  : {changed}")
        if deleted: print(f"    Deleted  : {deleted}")
        if moved: print(f"    Moved    : {moved}")
        if private: print(f"    Private  : {private}")
        print()
        for line in pending_lines[:20]:
            print(f"    {line.strip()}")
        if total > 20:
            print(f"    ... and {total - 20} more files")
    else:
        print("  Pending : (clean)")

    # Recent changesets
    print()
    print("-" * 50)
    print("  Recent Changesets (last 5)")
    print("-" * 50)
    log_out, _, _ = run_cm(['log', '-n', '5', '--format={changeset}: {comment}'])
    for line in log_out.split('\n')[:5]:
        if line.strip():
            print(f"  {line.strip()}")

    print()
    print("=" * 50)

if __name__ == '__main__':
    main()
