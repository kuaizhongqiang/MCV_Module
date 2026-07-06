# Plastic SCM Changeset Formatter

import subprocess
import sys
import argparse
import textwrap

VALID_TYPES = ['feat', 'fix', 'refactor', 'docs', 'chore', 'style', 'test']

def run_cm(args):
    """Run a cm command."""
    try:
        result = subprocess.run(['cm'] + args, capture_output=True, text=True, encoding='utf-8', timeout=30)
        return result.stdout.strip(), result.stderr.strip(), result.returncode
    except FileNotFoundError:
        print("[ERROR] cm CLI not found. Is Plastic SCM installed and in PATH?")
        sys.exit(1)

def format_message(change_type, summary, detail=None):
    """Format a changeset message following conventions."""
    if change_type not in VALID_TYPES:
        print(f"[WARN] Unknown type '{change_type}'. Use one of: {', '.join(VALID_TYPES)}")

    message = f"{change_type}: {summary}"

    if detail:
        message += f"\n\n{detail}"

    return message

def main():
    parser = argparse.ArgumentParser(
        description='Format and commit a Plastic SCM changeset with conventions.',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=textwrap.dedent(f"""\
            Examples:
              plastic_changeset.py feat "add scene loading pipeline"
              plastic_changeset.py fix "resolve UI panel z-order"
              plastic_changeset.py docs "update coding conventions" --dry-run
            Valid types: {', '.join(VALID_TYPES)}
        """)
    )
    parser.add_argument('type', choices=VALID_TYPES, help='Changeset type prefix')
    parser.add_argument('summary', help='Short summary (one line)')
    parser.add_argument('--detail', '-d', help='Detailed description')
    parser.add_argument('--all', '-a', action='store_true', help='Commit all pending changes')
    parser.add_argument('--file', '-f', nargs='+', help='Specific file(s) to commit')
    parser.add_argument('--dry-run', action='store_true', help='Print message only, do not commit')
    args = parser.parse_args()

    message = format_message(args.type, args.summary, args.detail)

    print("=" * 60)
    print("  Changeset Message")
    print("=" * 60)
    print(message)
    print("=" * 60)

    if args.dry_run:
        print("\n[Dry run] No changeset created.")
        sys.exit(0)

    # Build checkin command
    cmd = ['checkin', '-c', message]
    if args.all:
        cmd.append('--all')
    elif args.file:
        cmd.extend(args.file)
    else:
        print("\n[ERROR] Specify --all or --file to commit.")
        print("Use --dry-run to preview message only.")
        sys.exit(1)

    print(f"\nCommitting changeset...")
    _, stderr, rc = run_cm(cmd)

    if rc == 0:
        print("[OK] Changeset created successfully.")
    else:
        print(f"[ERROR] Failed to create changeset.")
        if stderr:
            print(stderr)
        sys.exit(1)

if __name__ == '__main__':
    main()
