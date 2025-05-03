import json
import argparse
import os
from collections import defaultdict

def is_mostly_english(text, threshold=0.7):
    letters = [c for c in text if c.isalpha()]
    if not letters:
        return False
    english_letters = [c for c in letters if 'a' <= c.lower() <= 'z']
    return len(english_letters) / len(letters) >= threshold

def build_markov_brain(text):
    transitions = defaultdict(list)
    lines = text.splitlines()

    for line in lines:
        if not is_mostly_english(line):
            continue
        if line.strip().startswith("!"):
            continue

        words = line.strip().split()
        if len(words) < 3:
            continue

        for i in range(len(words) - 2):
            key = f"{words[i]}|{words[i+1]}"
            transitions[key].append(words[i+2])

    return transitions

def main():
    parser = argparse.ArgumentParser(description="Build a Markov brain from a text file.")
    parser.add_argument("input", help="Path to input .txt file")
    parser.add_argument("channel", help="Channel name (used to name the output brain file)")
    args = parser.parse_args()

    if not os.path.exists(args.input):
        print(f"❌ Input file not found: {args.input}")
        return

    try:
        with open(args.input, 'r', encoding='utf-8') as f:
            text = f.read()
    except UnicodeDecodeError:
        print("⚠️ UTF-8 decode failed, trying Windows-1252 fallback...")
        with open(args.input, 'r', encoding='windows-1252') as f:
            text = f.read()

    transitions = build_markov_brain(text)
    output_dir = os.path.join(os.path.expanduser("~"), "Documents", "MiniBot")
    os.makedirs(output_dir, exist_ok=True)

    output_path = os.path.join(output_dir, f"markov_brain_{args.channel.lower()}.json")
    with open(output_path, 'w', encoding='utf-8') as out_file:
        json.dump(transitions, out_file, indent=2)

    print(f"✅ Markov brain saved to: {output_path}")
    print(f"🧠 Total keys: {len(transitions)}")


if __name__ == "__main__":
    main()
