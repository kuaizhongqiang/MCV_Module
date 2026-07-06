# Plastic SCM Branch Creator

import subprocess
import sys
import argparse

BRANCH_TYPES = ['feature', 'fix']

def run_cm(args, allow_fail=False):
    """Run a cm command."""
    try:
        result = subprocess.run(['cm'] + args, capture_output=True, text=True, encoding='utf-8', timeout=30)
        if result.returncode != 0 and not allow_fail:
            if result.stderr:
                print(f"[ERROR] {result.stderr.strip()}")
            return result.stdout, result.stderr, result.returncode
        return result.stdout.strip() if result.stdout else '', result.stderr.strip() if result.stderr else '', result.returncode
    except FileNotFoundError:
        print("[ERROR] cm CLI not found. Is Plastic SCM installed and in PATH?")
        sys.exit(1)

def ensure_parent_branch(branch_type):
    """Ensure the parent branch (e.g. /feature) exists, create if missing."""
    parent = f'/{branch_type}'
    out, _, rc = run_cm(['branch', 'list', '--format={name}'], allow_fail=True)
    if out and parent in out:
        return
    print(f"Creating parent branch: {parent}")
    _, _, rc = run_cm(['branch', 'create', parent], allow_fail=True)
    if rc != 0:
        print(f"[WARN] Could not create parent branch {parent}, continuing anyway...")

def validate_name(name):
    """Validate branch name follows conventions."""
    if not name:
        print("[ERROR] Branch name cannot be empty.")
        sys.exit(1)
    if ' ' in name:
        print("[ERROR] Branch name must not contain spaces. Use hyphens.")
        sys.exit(1)
    if name.startswith('/'):
        print("[ERROR] Do not include leading slash. Use: scene-loader (not /feature/scene-loader)")
        sys.exit(1)
    return name.replace(' ', '-').lower()

def main():
    parser = argparse.ArgumentParser(description='Create a Plastic SCM branch with conventions.')
    parser.add_argument('type', choices=BRANCH_TYPES, help='Branch type')
    parser.add_argument('name', help='Branch name (use hyphens, e.g. scene-loader)')
    parser.add_argument('--no-switch', action='store_true', help='Create but do not switch')
    args = parser.parse_args()

    name = validate_name(args.name)

    full_branch = f"/{args.type}/{name}"
    print(f"Creating branch: {full_branch}")

    # Ensure parent branch exists
    ensure_parent_branch(args.type)

    # Create branch from current changeset
    cmd = ['branch', 'create', full_branch]

    _, _, rc = run_cm(cmd)
    if rc != 0:
        print(f"[ERROR] Failed to create branch: {full_branch}")
        sys.exit(1)

    print(f"[OK] Branch created: {full_branch}")

    # Switch to new branch
    if not args.no_switch:
        print(f"Switching to: {full_branch}")
        _, _, rc = run_cm(['switch', full_branch])
        if rc != 0:
            print(f"[ERROR] Failed to switch to: {full_branch}")
            sys.exit(1)
        print(f"[OK] Now on: {full_branch}")

if __name__ == '__main__':
    main()
